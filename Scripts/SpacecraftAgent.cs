using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpacecraftAgent : Agent
{
    public GameObject spaceStation;
    public GameObject spacecraft;
    public GameObject spaceGargabe;
    Rigidbody rbSpacecraft;

    // Hyperparameters
    private Vector3 targetPosition;
    private Quaternion targetOrientation; // (x, y, z, w)
    private float positionTolerance = 1.5f;
    private float orientTolerance = 0.0055f * 5;  // 1 degree = 0.0055
    private float velocityTorlerance = 0.5f;
    private float angularVelocityTolerence = 0.08f;
    private float orientateSpeed = 0.5f;
    private float movementSpeed = 2.0f;
    private float maxVelocity = 10.0f;
    private float maxAngularVelocity = 3.0f;
    private float firstStageDistance = 10.0f;
    private float secondStageDistnace = 5.0f;
    private float scale = 0.01f;

    private bool isTrigger = false;
    const float degree2Rad = 0.0174533f; // pi/180
    static private float initPosRange = 20f;
    static private float rLimit = Mathf.Sqrt(Mathf.Pow(3 * initPosRange, 2)); // (2*initPosRange* root(2))^2 -> the possible largest initialization distance

    private float previousR = rLimit;
    private float previousOrientationDiff = 1;

    // Tracing
    [SerializeField]
    private UnityEngine.UI.Text text;
    [SerializeField]
    private UnityEngine.UI.Text text_stage;
    private float successCount = 0;
    private float failureCount = 0;
    private int stepsCount = 0;
    private int collisionCount = 0;
    private bool initialStage = false;
    private bool firstStage = false;
    private bool secondStage = false;


    public override void InitializeAgent()
    {
        rbSpacecraft = spacecraft.GetComponent<Rigidbody>();
        if(rbSpacecraft == null)
        {
            Debug.LogError("Rigid body could not be found.");
        }
        // Add offset from the space station
        targetPosition = spaceStation.transform.position + new Vector3(0, 0, -6); // (0, 0, -6) is the offset from space staion
        targetOrientation = spaceStation.transform.rotation; 
    }

    public override void CollectObservations()
    {
        AddVectorObs(spacecraft.transform.rotation.y); //1
        AddVectorObs(rbSpacecraft.velocity); //3 
        AddVectorObs(spacecraft.transform.position - spaceStation.transform.position); //3
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
        float r = Vector3.Distance(spacecraft.transform.position, targetPosition);// distance from spacecraft to target position (space station + offset);
        float orientationDiff = Mathf.Abs(spacecraft.transform.rotation.y - spaceStation.transform.rotation.y);
        int action = (int)vectorAction[0];

        // forward and backward
        if(action == 0 || action == 1)
        {
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
            stepsCount++;
            if (action == 3)
            {
                Orientation(rbSpacecraft, 1.0f * orientateSpeed);
                ClampAngularVelocity();
            }
            else
            {
                Orientation(rbSpacecraft,- 1.0f * orientateSpeed);
                ClampAngularVelocity();
            }
        }

        //-------------------------------------------------Reward fucntion ----------------------------------------------------------
        /* 
        * When spaceship distance is closer to the station -> reward!
        * The reward is propotional to the distance (the closer the higher)
        */

        // TODO: Need to Refactor! The nested condisiton is ugly.
        if (r > firstStageDistance)
        {
            // [Initial stage]: start to approach the station
            initialStage = true;
            firstStage = false;
            secondStage = false;
            float initialScale = 0.01f;
            PositionReward(r, initialScale);
        }
        else
        {     
            // [Second stage]: start to deaccelerate
            // TODO: De-acceleration and make sure the angle is correct
            if(r < secondStageDistnace)
            {
                initialStage = false;
                firstStage = false;
                secondStage = true;
                float secondStageScale = 0.1f;
                float secondStageOrientationScale = 2.0f;
                PositionReward(r, secondStageScale);
                OrientationReward(orientationDiff, secondStageOrientationScale);

            }
            else
            {
                // [First stage]: start to adjust the orientation
                // TODO: Align the spacecraft orientation to the space station orientation
                initialStage = false;
                firstStage = true;
                secondStage = false;
                float firstStagePositionScale = 0.05f;
                float firstStageOrientationScale = 1.0f;
                PositionReward(r, firstStagePositionScale);
                OrientationReward(orientationDiff, firstStageOrientationScale);
            }
        }

        /*
         * Punish for too many steps (above 200 steps)
         */
        if (stepsCount > 1000) //TBD
        {
            AddReward(-stepsCount * 0.01f); //TBD, The punish is propotional to the steps
        }

        // Failure: over the rLmint or hit space garbage
        if (r >= rLimit || isTrigger == true || stepsCount > 3000)
        {
            Done();
            SetReward(-1.0f);
            failureCount++;
            return;
        }

        // Success
        if (IsDock(r, spacecraft.transform.rotation, targetOrientation, rbSpacecraft))
        {
            Done();
            SetReward(1.0f);
            successCount++;
            return;
        }
        
        //-------------------------------------------------Reward fucntion ----------------------------------------------------------

        // Tracing
        if (text != null)
        {
            text.text = string.Format("[spacecraft] pos: ({0}, {1}, {2}), Orient Y {3}, Vel {4}, AngularVel {5}" +
                ", [target] pos: ({6}, {7}, {8}), Orient Y {9}" +
                ", distance: {10}" +
                ", reward: {11}, total reward:{12}, success: {13}/failure: {14}, successRate:{15}, steps:{16}, collisions:{17}"
                ,spacecraft.transform.position.x, spacecraft.transform.position.y, spacecraft.transform.position.z, spacecraft.transform.rotation.y, rbSpacecraft.velocity.magnitude ,rbSpacecraft.angularVelocity.magnitude
                ,targetPosition.x, targetPosition.y, targetPosition.z, targetOrientation.y
                , r
                , GetReward(), GetCumulativeReward(), successCount, failureCount, (successCount / (successCount + failureCount)) * 100, stepsCount, collisionCount);
        }

        if (text_stage != null)
        {
            text_stage.text = string.Format("[Stage]: Initial: {0}, First: {1}, Second: {2}",initialStage, firstStage, secondStage);
        }

        // Update the position and orientation.
        previousR = r;
        previousOrientationDiff = orientationDiff;
    }

    // Trigger Event
    void OnTriggerEnter(Collider other)
    {
        Debug.Log(gameObject.name + " was triggered by " + other.gameObject.name);
        isTrigger = true;
        collisionCount++;
    }

    public override void AgentReset()
    {
        spacecraft.transform.position = new Vector3(Random.Range(-initPosRange, initPosRange), 0f, Random.Range(-initPosRange, initPosRange));    
        spaceStation.transform.position = new Vector3(0f, 0f, 0f);
        spaceGargabe.transform.position = spaceStation.transform.position + new Vector3(6f, 0f, 8f); //related posisiton to 
        spacecraft.transform.position  = spaceStation.transform.position + new Vector3(0, 0, -8); // for test
        targetPosition = spaceStation.transform.position + new Vector3(0, 0, -6); // (0, 0, -6) is the offset from space staion
        targetOrientation = spaceStation.transform.rotation;

        rbSpacecraft.velocity = new Vector3(0f, 0f, 0f);
        rbSpacecraft.angularVelocity = new Vector3(0f, 0f, 0f);

        initialStage = false;
        firstStage = false;
        secondStage = false;
        isTrigger = false;
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
        rbSpacecraft.AddForce(transform.forward * amount, ForceMode.Acceleration);
    }

    private void Orientation(Rigidbody t, float amount)
    {
        t.AddTorque(0, amount, 0);

    }

    // Reward functions
    private void PositionReward(float r, float rewardScale)
    {
        float rewardPosition = (rLimit - r) * rewardScale;

        //Exclude qual 
        if (r < previousR)
            AddReward(rewardPosition);
        if (r > previousR)
            AddReward(-rewardPosition);   
    }

    // Based on the tracing information (spacecraft.transform.rotation.y)
    // -1 < Orientation.y < 1. Documentation: https://docs.unity3d.com/ScriptReference/Quaternion-y.html
    private void OrientationReward(float orientationDiff, float rewardScale)
    {
        // If diff = 0, reward, if diff = 1, punish ----> cosine function
        // If 1 Quaternion = 180 degree and 2pi radian = 180 degree  --> 1 Quaternion = 2 pi radian
        float rewardOrientation = Mathf.Cos(orientationDiff/ 2*Mathf.PI) * rewardScale; //TBD
        rewardOrientation = Mathf.Abs(rewardOrientation); // Make it positive

        if (orientationDiff < previousOrientationDiff)
            AddReward(rewardOrientation);
        if(orientationDiff > previousOrientationDiff)
            AddReward(-rewardOrientation);
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
        if (Mathf.Abs(spacecraftOrient.y - targetOrientation.y) < orientTolerance)
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