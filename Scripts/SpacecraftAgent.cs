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
    public GameObject guidance2_left;
    public GameObject guidance2_right;
    public GameObject guidance3_left;
    public GameObject guidance3_right;
    public GameObject guidance_side;
    public GameObject guidance_side_1;
    public GameObject guidance_side_2;
    public GameObject guidance_side_3;

    Rigidbody rbSpacecraft;
    RayPerception rayPer;

    // Hyperparameters
    private static float positionTolerance = 1.5f;
    private static float orientTolerance = 0.0055f * 5;  // 1 degree = 0.0055
    private static float velocityTorlerance = 1.0f;
    private static float angularVelocityTolerence = 0.08f;
    private static float orientateSpeed = 5f; // rigid body
    private static int orientationAngle = 2; // non-rigid body
    private static float momentForce = 5f; //Force
    private static float maxVelocity = 1.5f;
    private static float maxAngularVelocity = 3.0f;

    private bool isMove = false;
    private bool attitudeControlStart = false;
    private static float initPosRange = 50f;
    private static float rLimit = Mathf.Sqrt(Mathf.Pow(2 * initPosRange, 2)); // (2*initPosRange* root(2))^2 -> the possible largest initialization distance

    private float previousR = rLimit; // maximum value
    private float previousOrientationDiff = 1; // maximum value
    private float previousVelocity = maxVelocity; // maximum value

    // Tracing
    [SerializeField]
    private UnityEngine.UI.Text text;
    [SerializeField]
    private UnityEngine.UI.Text textAttitudeControl;
    [SerializeField]
    private UnityEngine.UI.Text textResult;
    private float successCount = 0;
    private float perfectCount = 0;
    private float failureCount = 0;
    private int stepsCount = 0;
    private float oritentationReward = 0;
    private float velocityReward = 0;


    public override void InitializeAgent()
    {
        FloatingTextController.Initialize();
        rbSpacecraft = spacecraft.GetComponent<Rigidbody>();
        if (rbSpacecraft == null)
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
        float[] rayAngles = {80f, 83f, 86f, 90f, 93f, 96f,100f};
        string[] detectableObjects = { "spaceStation", "spaceGarbage", "wall", "dockingPoint", "guidance_1", "guidance_2", "guidance_3", "guidance_side" };
        AddVectorObs(rayPer.Perceive(rayDistance, rayAngles, detectableObjects, 0f, 0f)); // 48!?
        AddVectorObs((float)GetStepCount() / (float)agentParameters.maxStep);//1
        SetTextObs("Testing " + gameObject.GetInstanceID());
    }
    /*
     * 0: Up    Arrow: positive z 
     * 1: Down  Arrow: negative z
     * 2: left  Arrow: positive rotation y
     * 3: right Arrow: negative rotation y
     * 
     */
    public override void AgentAction(float[] vectorAction, string textAction)
    {
        float r = Vector3.Distance(spacecraft.transform.position, dockingPoint.transform.position);// distance from spacecraft to target position (space station + offset);
        float orientationDiff = Mathf.Abs(spacecraft.transform.rotation.y - dockingPoint.transform.rotation.y);
        float velocity = rbSpacecraft.velocity.magnitude;

        int action = (int)vectorAction[0];
        
        if(!attitudeControlStart)
            AddReward(-1f / agentParameters.maxStep);

        if (action != -1) // default action = -1 in Unity inspector
        {
            isMove = true;
            stepsCount++;

            // Foward
            if (action == 0)
            {
                ThrustForward(momentForce);
                ClampVelocity();
            }
            // Backward
            if (action == 1)
            {
                ThrustForward(-momentForce);
                ClampVelocity();
            }
            // Position y rotation
            if (action == 2)
            {
                //Orientation(rbSpacecraft,- 1.0f * orientateSpeed);
                spacecraft.transform.Rotate(Vector3.up, -orientationAngle);
                ClampAngularVelocity();
            }
            // Negative y rotation
            if (action == 3)
            {
                //Orientation(rbSpacecraft, 1.0f * orientateSpeed);
                spacecraft.transform.Rotate(Vector3.up, orientationAngle);
                ClampAngularVelocity();
            }

        }

        /*
         * Start to execute attitude control
         */
        if (attitudeControlStart)
        {
            //1. Control orientation
            if (orientationDiff < 0.3) //TBD
            {
                float scale = 0.001f;
                if(orientationDiff < 0.07)
                {
                    scale = 0.01f;
                }
                float reward = (0.3f - orientationDiff) * scale; 
                AddReward(reward);
                oritentationReward = reward;
            }
            else
            {
                float reward = (0.3f - orientationDiff) * 0.005f;
                AddReward(reward);
                oritentationReward = reward;
            }

            //if (velocity < 1.0f)
            //{
            //    float reward = (2 - velocity) * 0.005f; // The slower the higher
            //    AddReward(reward);
            //    velocityReward = reward;
            //}
            //else
            //{
            //    AddReward(-0.001f);
            //    velocityReward = -0.001f;
            //}
        }

        // Tracing
        if (text != null)
        {
            text.text = string.Format("[spacecraft] pos: ({0}, {1}, {2}), Orient Y {3}, Vel {4}, AngularVel {5}" +
                ", distance: {6}" +
                ", reward: {7}, total reward:{8}, success: {9}/failure: {10}, successRate:{11}%, steps:{12}, perfect: {13}"
                , spacecraft.transform.position.x, spacecraft.transform.position.y, spacecraft.transform.position.z, spacecraft.transform.rotation.y, rbSpacecraft.velocity.magnitude, rbSpacecraft.angularVelocity.magnitude
                , r
                , GetReward(), GetCumulativeReward(), successCount, failureCount, (successCount / (successCount + failureCount)) * 100, stepsCount, perfectCount);
        }

        if(textAttitudeControl != null)
        {
            textAttitudeControl.text = string.Format("[Attitude control] start:{0}, orientationDiff: {1}, orientationReward:{2}, velocityReward{3}, total reward: {4}"
                , attitudeControlStart, orientationDiff, oritentationReward, velocityReward, oritentationReward + velocityReward);
        }

        // Update the position and orientation.
        previousR = r;
        previousVelocity = velocity;
        previousOrientationDiff = orientationDiff;
        isMove = false;
    }

    // Trigger Event
    void OnTriggerEnter(Collider other)
    {
        Debug.Log(gameObject.name + " was triggered by " + other.gameObject.name);


        if (other.gameObject.CompareTag("guidance_side"))
        {
            AddReward(0.1f);
            other.gameObject.SetActive(false);

        }

        if (other.gameObject.CompareTag("guidance_3"))
        {
            attitudeControlStart = true; // Start to perform altitude control
            maxVelocity = 0.6f; // lock the speed
            orientationAngle = 1; // lock the orientation
            AddReward(8f);
            other.gameObject.SetActive(false);
            if (other.gameObject == guidance3_right)
            {
                guidance3_left.SetActive(false); // deactivate the other guidance3
                guidance2_right.SetActive(true);
            }
            if (other.gameObject == guidance3_left)
            {
                guidance3_right.SetActive(false);
                guidance2_left.SetActive(true);
            }
            guidance1.SetActive(true);
        }

        if (other.gameObject.CompareTag("guidance_2"))
        {
            AddReward(8f);
            other.gameObject.SetActive(false);
        }

        if (other.gameObject.CompareTag("guidance_1"))
        {
            AddReward(8f);
            other.gameObject.SetActive(false);
            dockingPoint.SetActive(true);
        }

        if (other.gameObject.CompareTag("dockingPoint"))
        {
            successCount++;
            if (IsPerfectDock())
            {
                perfectCount++;
                AddReward(100f); //TBD
                FloatingTextController.CreateFloatingText("Perfect docking!", transform);
            }

            else
            {
                AddReward(5f); //TBD
                FloatingTextController.CreateFloatingText("Well done!", transform);
            }
            Done();
        }
    }

    // Collision Evenet
    void OnCollisionEnter(Collision col)
    {
        Debug.Log(gameObject.name + " was collided by " + col.gameObject.name);

        failureCount++;
        AddReward(-50f);

        FloatingTextController.CreateFloatingText("Failed!", transform);
        
        Done();
    }

    public override void AgentReset()
    {
        spacecraft.transform.position = new Vector3(Random.Range(-initPosRange, initPosRange), 0f, Random.Range(-initPosRange, initPosRange));
        //spaceStation.transform.position = new Vector3(Random.Range(-initPosRange, initPosRange), 0f, Random.Range(-initPosRange, initPosRange));
        spaceStation.transform.position = new Vector3(0f, 0f, 0f);
        //spacecraft.transform.position  = spaceStation.transform.position + new Vector3(0, 0, -20); // for test
        dockingPoint.transform.position = spaceStation.transform.position + new Vector3(0, 0, -3.4f); // (0, 0, -3.4f) is the offset from space staion
        dockingPoint.transform.rotation = spaceStation.transform.rotation;

        spaceStation.transform.rotation = new Quaternion(0f, 0f, 0f, 0f);
        dockingPoint.transform.rotation = new Quaternion(0f, 0f, 0f, 0f);


        guidance1.SetActive(false);
        guidance2_left.SetActive(false);
        guidance2_right.SetActive(false);
        guidance3_left.SetActive(true);
        guidance3_right.SetActive(true);
        guidance_side.SetActive(true);
        guidance_side_1.SetActive(true);
        guidance_side_2.SetActive(true);
        guidance_side_3.SetActive(true);
        dockingPoint.SetActive(false);


        guidance1.transform.position = spaceStation.transform.position + new Vector3(0f, 0, -5.8f);
        guidance2_right.transform.position = spaceStation.transform.position + new Vector3(0.9f, 0, -7.8f);
        guidance2_left.transform.position = spaceStation.transform.position + new Vector3(-0.9f, 0, -7.8f);
        guidance3_right.transform.position = spaceStation.transform.position + new Vector3(2.1f, 0, -9.8f);
        guidance3_left.transform.position = spaceStation.transform.position + new Vector3(-2.1f, 0, -9.8f);
        guidance_side.transform.position = spaceStation.transform.position + new Vector3(-10.0f, 0, 0f);

        rbSpacecraft.velocity = new Vector3(0f, 0f, 0f);
        rbSpacecraft.angularVelocity = new Vector3(0f, 0f, 0f);

        if (textResult != null)
        {
            textResult.text = string.Format("Start!");
        }

        isMove = false;
        attitudeControlStart = false;
        maxVelocity = 1.5f; // un-lock the speed
        orientationAngle = 2; // un-lock the orientation
        stepsCount = 0;
        oritentationReward = 0;
        velocityReward = 0;
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
        }

        if (isPunish)
        {
            if (r > previousR)
            {
                AddReward(-rewardPosition);
            }
        }
    }

    // -1 < Orientation.y < 1. Documentation: https://docs.unity3d.com/ScriptReference/Quaternion-y.html
    private void OrientationReward(float orientationDiff, float rewardScale, bool isPunish)
    {
        // If diff = 0, reward, if diff = 1, punish ----> cosine function
        // If 1 Quaternion = 180 degree and pi radian = 180 degree  --> 1 Quaternion =  pi radian
        float rewardOrientation = Mathf.Abs(Mathf.Cos(orientationDiff / Mathf.PI)) * rewardScale; //TBD

        if (orientationDiff < previousOrientationDiff)
        {
            AddReward(rewardOrientation);
        }

        if (isPunish)
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
}