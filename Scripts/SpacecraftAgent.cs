using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpacecraftAgent : Agent
{
    public GameObject spaceStation;
    public GameObject spacecraft;
    public GameObject spaceGargabe;
    public GameObject dockingPoint;
    public GameObject guidance1;
    public GameObject guidance2;
    public GameObject guidance3;
    public GameObject guidance4;

    Rigidbody rbSpacecraft;
    RayPerception rayPer;

    // Hyperparameters
    private float positionTolerance = 1.5f;
    private float orientTolerance = 0.0055f * 5;  // 1 degree = 0.0055
    private float velocityTorlerance = 0.5f;
    private float angularVelocityTolerence = 0.08f;
    private float orientateSpeed = 5f;
    private int orientationAngle = 2;
    private float movementSpeed = 2.0f;
    private float maxVelocity = 3.0f;
    private float maxAngularVelocity = 3.0f;
    private float firstStageDistance = 10.0f;
    private float secondStageDistnace = 5.0f;
    private float scale = 0.01f;

    private bool isTrigger = false;
    private bool isMove = false;
    static private float initPosRange = 30f;
    static private float rLimit = Mathf.Sqrt(Mathf.Pow(2 * initPosRange, 2)); // (2*initPosRange* root(2))^2 -> the possible largest initialization distance

    private float previousR = rLimit; // maximum value
    private float previousOrientationDiff = 1; // maximum value
    private float previousPositonOrientationDiff = 180; // maximum vale

    // Tracing
    [SerializeField]
    private UnityEngine.UI.Text text;
    [SerializeField]
    private UnityEngine.UI.Text text_result;
    private float successCount = 0;
    private float failureCount = 0;
    private int stepsCount = 0;
    private float positionReward = 0;



    public override void InitializeAgent()
    {
        rbSpacecraft = spacecraft.GetComponent<Rigidbody>();
        if(rbSpacecraft == null)
        {
            Debug.LogError("Rigid body could not be found.");
        }
        rayPer = GetComponent<RayPerception>();
        if (rayPer == null)
        {
            Debug.LogError("RayPerception could not be found.");
        }
    }

    public override void CollectObservations()
    {
        float rayDistance = 80f;
        float[] rayAngles = {60f, 70f ,80f, 90f, 100f, 110f, 120f};
        string[] detectableObjects = { "spaceStation", "spaceGarbage", "wall" ,"dockingPoint", "guidance"};
        AddVectorObs(rayPer.Perceive(rayDistance, rayAngles, detectableObjects, 0f, 0f)); // 48!?
        AddVectorObs((float)GetStepCount() / (float)agentParameters.maxStep);//1
        SetTextObs("Testing " + gameObject.GetInstanceID());
    }
    /*
     * 0: Up    Arrow: positive z 
     * 1: Down  Arrow: negative z 
     * 2: Q     Key:   positive rotation y
     * 3: W     Key:   negative rotation y
     * 
     */
    public override void AgentAction(float[] vectorAction, string textAction)
    {
        float r = Vector3.Distance(spacecraft.transform.position, dockingPoint.transform.position);// distance from spacecraft to target position (space station + offset);
        int action = (int)vectorAction[0];

        AddReward(-1f / agentParameters.maxStep);
        // forward and backward
        if (action == 0 || action == 1)
        {
            isMove = true;
            stepsCount++;
            if (action ==0)
            {
                ThrustForward(movementSpeed);
                ClampVelocity();
            }
            else
            {
                ThrustForward(-movementSpeed);
                ClampVelocity();
            }
            
        }

        // rotation
        if (action == 2 || action == 3)
        {
            isMove = true;
            stepsCount++;
            if (action == 3)
            {
                //Orientation(rbSpacecraft, 1.0f * orientateSpeed);
                spacecraft.transform.Rotate(Vector3.up, orientationAngle);
                ClampAngularVelocity();
            }
            else
            {
                //Orientation(rbSpacecraft,- 1.0f * orientateSpeed);
                spacecraft.transform.Rotate(Vector3.up, -orientationAngle);
                ClampAngularVelocity();
            }
        }

        //-------------------------------------------------Reward fucntion ----------------------------------------------------------
        /* 
        * When spaceship distance is closer to the station -> reward!
        * The reward is propotional to the distance (the closer the higher)
        */

        //// TODO: Need to Refactor! The nested condisiton is ugly.
        //if (r > firstStageDistance)
        //{
        //    // [Initial stage]: start to approach the station
        //    initialStage = true;
        //    firstStage = false;
        //    secondStage = false;
        //    float initialScale = 0.005f;
        //    PositionReward(r, initialScale, true);
        //    //PositionOrientationReward(positionOrientationDiff, initialScale, true);
        //}
        //else
        //{     
        //    // [Second stage]: start to deaccelerate
        //    // TODO: De-acceleration and make sure the angle is correct
        //    if(r < secondStageDistnace)
        //    {
        //        initialStage = false;
        //        firstStage = false;
        //        secondStage = true;
        //        float secondStageScale = 0.05f;
        //        float secondStageOrientationScale = 2.0f;
        //        PositionReward(r, secondStageScale, false);
        //        OrientationReward(orientationDiff, secondStageOrientationScale, false);
        //        PositionOrientationReward(positionOrientationDiff, secondStageOrientationScale, false);

        //    }
        //    else
        //    {
        //        // [First stage]: start to adjust the orientation
        //        // TODO: Align the spacecraft orientation to the space station orientation
        //        initialStage = false;
        //        firstStage = true;
        //        secondStage = false;
        //        float firstStagePositionScale = 0.01f;
        //        float firstStageOrientationScale = 1.0f;
        //        PositionReward(r, firstStagePositionScale, false);
        //        OrientationReward(orientationDiff, firstStageOrientationScale, true);
        //        PositionOrientationReward(positionOrientationDiff, firstStageOrientationScale, true);
        //    }
        //}

        ///*
        // * Punish for too many steps
        // */
        //if (isMove && stepsCount > 1500) 
        //{
        //    stepReward = -stepsCount * 0.0005f;
        //    AddReward(stepReward); //The punish is propotional to the steps
        //}

        //// Failure: over the rLmint or hit space garbage
        //if (r >= rLimit || isTrigger == true || stepsCount > 3000)
        //{
        //    Done();
        //    SetReward(-1.0f);
        //    failureCount++;
        //    return;
        //}

        //// Success
        //if (IsDock(r, spacecraft.transform.rotation, dockingPoint.transform.rotation, rbSpacecraft))
        //{
        //    Done();
        //    SetReward(1.0f);
        //    successCount++;
        //    return;
        //}
        
        //-------------------------------------------------Reward fucntion ----------------------------------------------------------

        // Tracing
        if (text != null)
        {
            text.text = string.Format("[spacecraft] pos: ({0}, {1}, {2}), Orient Y {3}, Vel {4}, AngularVel {5}" +
                ", distance: {6}" +
                ", reward: {7}, total reward:{8}, success: {9}/failure: {10}, successRate:{11}, steps:{12}"
                ,spacecraft.transform.position.x, spacecraft.transform.position.y, spacecraft.transform.position.z, spacecraft.transform.rotation.y, rbSpacecraft.velocity.magnitude ,rbSpacecraft.angularVelocity.magnitude
                , r
                , GetReward(), GetCumulativeReward(), successCount, failureCount, (successCount / (successCount + failureCount)) * 100, stepsCount);
        }

        // Update the position and orientation.
        previousR = r;
        isMove = false;
    }

    // Trigger Event
    void OnTriggerEnter(Collider other)
    {
        Debug.Log(gameObject.name + " was triggered by " + other.gameObject.name);
        isTrigger = true;
        if (other.gameObject.CompareTag("guidance"))
        {
            AddReward(1f);
            other.gameObject.SetActive(false);
            //Destroy(other.gameObject);
        }
    }

    // Collision Evenet
    void OnCollisionEnter(Collision col)
    {
        Debug.Log(gameObject.name + " was collided by " + col.gameObject.name);
        if(col.gameObject.CompareTag("dockingPoint"))
        {
            successCount++;
            AddReward(5f); //TBD
            if(text_result != null)
            {
                text_result.text = string.Format("Well done!");
            }
            Done();
        }

        else
        {
            failureCount++;
            AddReward(-1f);
            if (text_result != null)
            {
                text_result.text = string.Format("Failed!");
            }
            Done();
        }
        
    }

    public override void AgentReset()
    {
        spacecraft.transform.position = new Vector3(Random.Range(-initPosRange, initPosRange), 0f, Random.Range(-initPosRange, initPosRange));    
        spaceStation.transform.position = new Vector3(Random.Range(-initPosRange, initPosRange), 0f, Random.Range(-initPosRange, initPosRange));
        //spacecraft.transform.position  = spaceStation.transform.position + new Vector3(0, 0, -12); // for test
        dockingPoint.transform.position = spaceStation.transform.position + new Vector3(0, 0, -3.4f); // (0, 0, -3.4f) is the offset from space staion
        dockingPoint.transform.rotation = spaceStation.transform.rotation;

        guidance1.SetActive(true);
        guidance2.SetActive(true);
        guidance3.SetActive(true);
        guidance4.SetActive(true);
        guidance1.transform.position = spaceStation.transform.position + new Vector3(0, 0, -5f);
        guidance2.transform.position = spaceStation.transform.position + new Vector3(0, 0, -6.5f);
        guidance3.transform.position = spaceStation.transform.position + new Vector3(0, 0, -8f);
        guidance4.transform.position = spaceStation.transform.position + new Vector3(0, 0, -9.5f);

        rbSpacecraft.velocity = new Vector3(0f, 0f, 0f);
        rbSpacecraft.angularVelocity = new Vector3(0f, 0f, 0f);

        isTrigger = false;
        isMove = false;
        SetReward(0);
        stepsCount = 0;
        
    }

    // Helper functions
    private void ClampVelocity()
    {
        float x = Mathf.Clamp(rbSpacecraft.velocity.x, -maxVelocity, maxVelocity);
        float z = Mathf.Clamp(rbSpacecraft.velocity.z, -maxVelocity, maxVelocity);

        rbSpacecraft.velocity = new Vector3(x, 0, z);
    }

    private void ClampAngularVelocity()
    {
        float y = Mathf.Clamp(rbSpacecraft.angularVelocity.y, -maxAngularVelocity, maxAngularVelocity);

        rbSpacecraft.angularVelocity = new Vector3(0, y, 0);
    }

    private void ThrustForward(float amount)
    {
        rbSpacecraft.AddForce(transform.forward * amount, ForceMode.Acceleration);
    }

    private void Orientation(Rigidbody t, float amount)
    {
        t.AddTorque(0, amount, 0);

    }

    // Reward functions
    private void PositionReward(float r, float rewardScale, bool isPunish)
    {
        float rewardPosition = (rLimit - r) * rewardScale;

        //Exclude qual 
        if (r < previousR)
        {
            AddReward(rewardPosition);
            positionReward = rewardPosition; // for tracing
        }

        if (isPunish)
        {
            if (r > previousR)
            {
                AddReward(-rewardPosition);
                positionReward = -rewardPosition; // for tracing
            }
        }             
    }

    private void PositionOrientationReward(float positionOrientationDiff, float rewardScale, bool isPunish)
    {
        // 0 is the best, 180 is the worst -> cosince
        float rewardPositionOrientation = Mathf.Abs(Mathf.Cos(positionOrientationDiff / Mathf.PI)) * rewardScale;

        if (positionOrientationDiff < previousPositonOrientationDiff)
        {
            AddReward(rewardPositionOrientation);
        }
        
        if(isPunish)
        {
            if (positionOrientationDiff > previousPositonOrientationDiff)
            {
                AddReward(-rewardPositionOrientation);
            }
        }

    }

    // Based on the tracing information (spacecraft.transform.rotation.y)
    // -1 < Orientation.y < 1. Documentation: https://docs.unity3d.com/ScriptReference/Quaternion-y.html
    private void OrientationReward(float orientationDiff, float rewardScale, bool isPunish)
    {
        // If diff = 0, reward, if diff = 1, punish ----> cosine function
        // If 1 Quaternion = 180 degree and pi radian = 180 degree  --> 1 Quaternion =  pi radian
        float rewardOrientation = Mathf.Abs(Mathf.Cos(orientationDiff/ Mathf.PI)) * rewardScale; //TBD

        if (orientationDiff < previousOrientationDiff)
        {
            AddReward(rewardOrientation);
        }
        
        if(isPunish)
        {
            if (orientationDiff > previousOrientationDiff)
            {
                AddReward(-rewardOrientation);
            }
        }           
    }

    /*
     * Check 1. position 2.angle 3.velocity 4.angular velocity
     */
    private bool IsDock(float distance, Quaternion spacecraftOrient, Quaternion targetOrient, Rigidbody rigidbodySpacecraft)
    {
        bool result = true;
        bool isPositionValid = false;
        bool isOrientationValid = false;
        bool isVelocityValid = false;
        bool isAngularVelocityValid = false;

        float spacecraftVelocity = rigidbodySpacecraft.velocity.magnitude;
        float spacecraftAngularVelocity =  rigidbodySpacecraft.angularVelocity.magnitude;


        // 1. Distance is close enough
        if (distance < positionTolerance)
            isPositionValid = true;

        // 2. Orientation is close enough
        if (Mathf.Abs(spacecraftOrient.y - dockingPoint.transform.rotation.y) < orientTolerance)
            isOrientationValid = true;

        // 3. Spacecraft has very low velocity
        if (spacecraftVelocity < velocityTorlerance)
            isVelocityValid = true;

        // 4. Spacecraft has very low angular velocity
        if (spacecraftAngularVelocity < angularVelocityTolerence)
            isAngularVelocityValid = true;

        // Chain of tests
        result &= isPositionValid; 
        result &= isOrientationValid;
        result &= isVelocityValid;
        result &= isAngularVelocityValid;

        return result;
    }
}