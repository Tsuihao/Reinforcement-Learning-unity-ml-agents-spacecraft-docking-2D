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
    public GameObject guidance_side;

    Rigidbody rbSpacecraft;
    RayPerception rayPer;

    // Hyperparameters
    private float positionTolerance = 1.5f;
    private float orientTolerance = 0.0055f * 5;  // 1 degree = 0.0055
    private float velocityTorlerance = 1.0f;
    private float angularVelocityTolerence = 0.08f;
    private float orientateSpeed = 5f;
    private int orientationAngle = 2;
    private float movementSpeed = 5.0f; //Force
    private float maxVelocity = 10.0f;
    private float maxAngularVelocity = 3.0f;

    private bool isMove = false;
    static private float initPosRange = 30f;
    static private float rLimit = Mathf.Sqrt(Mathf.Pow(2 * initPosRange, 2)); // (2*initPosRange* root(2))^2 -> the possible largest initialization distance

    private float previousR = rLimit; // maximum value
    private float previousOrientationDiff = 1; // maximum value

    // Tracing
    [SerializeField]
    private UnityEngine.UI.Text text;
    [SerializeField]
    private UnityEngine.UI.Text text_result;
    private float successCount = 0;
    private float perfectCount = 0;
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
        float[] rayAngles = { 60f, 70f, 80f, 90f, 100f, 110f, 120f };
        string[] detectableObjects = { "spaceStation", "spaceGarbage", "wall" ,"dockingPoint", "guidance", "guidance2"};
        AddVectorObs(rayPer.Perceive(rayDistance, rayAngles, detectableObjects, 0f, 0f)); // 48!?
        AddVectorObs((float)GetStepCount() / (float)agentParameters.maxStep);//1
        SetTextObs("Testing " + gameObject.GetInstanceID());
    }
    /*
     * 0: Up    Arrow: positive z 
     * 2: left  Arrow: positive rotation y
     * 3: right Arrow: negative rotation y
     * 
     */
    public override void AgentAction(float[] vectorAction, string textAction)
    {
        float r = Vector3.Distance(spacecraft.transform.position, dockingPoint.transform.position);// distance from spacecraft to target position (space station + offset);
        int action = (int)vectorAction[0];

        AddReward(-1f / agentParameters.maxStep);
        // forward and backward
        if (action == 0)
        {
            isMove = true;
            stepsCount++;

            ThrustForward(movementSpeed);
            ClampVelocity();      
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

        // Tracing
        if (text != null)
        {
            text.text = string.Format("[spacecraft] pos: ({0}, {1}, {2}), Orient Y {3}, Vel {4}, AngularVel {5}" +
                ", distance: {6}" +
                ", reward: {7}, total reward:{8}, success: {9}/failure: {10}, successRate:{11}%, steps:{12}, perfect: {13}"
                ,spacecraft.transform.position.x, spacecraft.transform.position.y, spacecraft.transform.position.z, spacecraft.transform.rotation.y, rbSpacecraft.velocity.magnitude ,rbSpacecraft.angularVelocity.magnitude
                , r
                , GetReward(), GetCumulativeReward(), successCount, failureCount, (successCount / (successCount + failureCount)) * 100, stepsCount, perfectCount);
        }

        // Update the position and orientation.
        previousR = r;
        isMove = false;
    }

    // Trigger Event
    void OnTriggerEnter(Collider other)
    {
        Debug.Log(gameObject.name + " was triggered by " + other.gameObject.name);

        if (other.gameObject.CompareTag("guidance2"))
        {
            AddReward(0.01f);
            other.gameObject.SetActive(false);
        }

        if (other.gameObject.CompareTag("guidance"))
        {
            AddReward(1f);
            other.gameObject.SetActive(false);
        }

        if (other.gameObject.CompareTag("dockingPoint"))
        {
            successCount++;
            if (IsPerfectDock())
            {
                perfectCount++;
                AddReward(100f); //TBD
                text_result.text = string.Format("Perfect docking!");
            }

            else
            {
                AddReward(5f); //TBD
                text_result.text = string.Format("Well done!");
            }
            Done();
        }


    }

    // Collision Evenet
    void OnCollisionEnter(Collision col)
    {
        Debug.Log(gameObject.name + " was collided by " + col.gameObject.name);

        failureCount++;
        AddReward(-1f);
        if (text_result != null)
        {
            text_result.text = string.Format("Failed!");
        }
        Done();
        
        
    }

    public override void AgentReset()
    {
        spacecraft.transform.position = new Vector3(Random.Range(-initPosRange, initPosRange), 0f, Random.Range(-initPosRange, initPosRange));    
        //spaceStation.transform.position = new Vector3(Random.Range(-initPosRange, initPosRange), 0f, Random.Range(-initPosRange, initPosRange));
        spaceStation.transform.position = new Vector3(0f, 0f, 0f);
        //spacecraft.transform.position  = spaceStation.transform.position + new Vector3(0, 0, -12); // for test
        dockingPoint.transform.position = spaceStation.transform.position + new Vector3(0, 0, -3.4f); // (0, 0, -3.4f) is the offset from space staion
        dockingPoint.transform.rotation = spaceStation.transform.rotation;

        spaceStation.transform.rotation = new Quaternion(0f, 0f, 0f, 0f);
        dockingPoint.transform.rotation = new Quaternion(0f, 0f, 0f, 0f);

        guidance1.SetActive(true);
        guidance2.SetActive(true);
        guidance3.SetActive(true);
        guidance_side.SetActive(true);
        guidance1.transform.position = spaceStation.transform.position + new Vector3(0, 0, -5f);
        guidance2.transform.position = spaceStation.transform.position + new Vector3(0, 0, -6.5f);
        guidance3.transform.position = spaceStation.transform.position + new Vector3(0, 0, -8f);
        guidance_side.transform.position = spaceStation.transform.position + new Vector3(-10.0f, 0, 0f);

        rbSpacecraft.velocity = new Vector3(0f, 0f, 0f);
        rbSpacecraft.angularVelocity = new Vector3(0f, 0f, 0f);

        if (text_result != null)
        {
            text_result.text = string.Format("Start!");
        }

        isMove = false;
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
     * Check 1.angle 2.velocity 3.angular velocity
     */
    private bool IsPerfectDock()
    {
        bool result = true;
        bool isOrientationValid = false;
        bool isVelocityValid = false;
        bool isAngularVelocityValid = false;

        //Extract infomation from rigidbody
        float spacecraftVelocity = rbSpacecraft.velocity.magnitude;
        float spacecraftAngularVelocity = rbSpacecraft.angularVelocity.magnitude;

        // 1. Orientation is close enough
        if (Mathf.Abs(spacecraft.transform.rotation.y - dockingPoint.transform.rotation.y) < orientTolerance)
            isOrientationValid = true;

        // 2. Spacecraft has very low velocity
        if (spacecraftVelocity < velocityTorlerance)
            isVelocityValid = true;

        // 3. Spacecraft has very low angular velocity
        if (spacecraftAngularVelocity < angularVelocityTolerence)
            isAngularVelocityValid = true;

        // Chain of tests
        result &= isOrientationValid;
        result &= isVelocityValid;
        result &= isAngularVelocityValid;

        return result;
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