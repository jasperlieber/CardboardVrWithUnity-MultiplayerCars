//-------------------------------------------------
//                    TNet 3
// Copyright © 2012-2016 Tasharen Entertainment Inc
//-------------------------------------------------

using UnityEngine;
using TNet;
using UnityEngine.UI;

/// <summary>
/// Extended car that adds TNet-based multiplayer support.
/// </summary>

[RequireComponent(typeof(TNObject))]
public class ExampleCar : ExampleCarNoNetworking
{
	/// <summary>
	/// Maximum number of updates per second when synchronizing input axes.
	/// The actual number of updates may be less if nothing is changing.
	/// </summary>

	[Range(1f, 20f)]
	public float inputUpdates = 10f;

	/// <summary>
	/// Maximum number of updates per second when synchronizing the rigidbody.
	/// </summary>

	[Range(0.25f, 5f)]
	public float rigidbodyUpdates = 1f;

	/// <summary>
	/// We want to cache the network object (TNObject) we'll use for network communication.
	/// If the script was derived from TNBehaviour, this wouldn't have been necessary.
	/// </summary>

	[System.NonSerialized]
	public TNObject tno;

	protected Vector2 mLastInput;
	protected float mLastInputSend = 0f;
	protected float mNextRB = 0f;

    private Text myText = null;

    protected override void Awake()
    {
        base.Awake();
        tno = GetComponent<TNObject>();

         if (GameObject.Find("MyText") != null)
            myText = GameObject.Find("MyText").GetComponent<Text>();

        if (myText != null) myText.text = "Debug\n";

    }


    private static bool btnDown = false;

    private int forwardOrReverse = 1;
    private Vector3 screenPos;
    private bool screenIsTouched;

    private bool CheckTouch()
    {
        if (Input.GetMouseButtonUp(0))
        {
            btnDown = false;
            forwardOrReverse = -forwardOrReverse;
        }
        if (Input.GetMouseButtonDown(0))
        {
            btnDown = true;
        }

        if (btnDown)
        {

            screenPos = Input.mousePosition;
        }

        screenIsTouched = Input.touchCount > 0;

        bool moveTransform = screenIsTouched || btnDown;// && !btnUp;

        return moveTransform;

        //if (moveTransform)
        //{
        //    //Debug.Log(transform.position.y);

        //    transform.position += /*myCamera.*/transform.forward * thrust * Time.deltaTime /* * (new Vector3(1,0,1))*/;

        //    transform.position = new Vector3(
        //        Mathf.Max(Mathf.Min(transform.position.x, bxmax), bxmin),
        //        Mathf.Max(Mathf.Min(transform.position.y, bymax), bymin),
        //        Mathf.Max(Mathf.Min(transform.position.z, bzmax), bzmin));
        //}
        //Debug.Log(moveTransform);
    }

    /// <summary>
    /// Only the car's owner should be updating the movement axes, and the result should be sync'd with other players.
    /// </summary>
    protected override void Update ()
	{
		// Only the player that actually owns this car should be controlling it
		if (!tno.isMine) return;

        if (CheckTouch())
        {
            mInput.y = forwardOrReverse;

            if (!screenIsTouched)
            {
                //Debug.Log("screenPos = " + screenPos.ToString() + ", screen.width = " + Screen.width);
                int screenMiddle = Screen.width / 2;
                mInput.x = (screenPos.x - screenMiddle) / screenMiddle;
            }
            else
            {
                //float myRotY = transform.eulerAngles.y + 360;
                //float camRotY = Camera.main.transform.eulerAngles.y + 360;
                float camRotZ = Camera.main.transform.eulerAngles.z;

                // assuming camRotZ is in [0,360] 
                if (camRotZ > 180)  mInput.x = -Mathf.Max(-1, (camRotZ - 360) / 20);
                else                mInput.x = -Mathf.Min(1, camRotZ / 20); 
                //else mInput.x = 0;


                //float diff = camRotY - myRotY;

                if (myText != null)
                    myText.text = "\nCamera eulerAngles = " + Camera.main.transform.eulerAngles.ToString()
                        + "\ncamRotZ = " + camRotZ + "\nmInput.x = " + mInput.x;

                //myText.text = txt + "\nmy eulerAngles = " + transform.eulerAngles.ToString()
                //    + "\nmyRotY = " + myRotY + "\ncamRotY = " + camRotY
                //    + "\ndiff = " + diff;

                //if (Mathf.Abs(diff) > 1f) mInput.x = Mathf.Sign(diff);

                //mInput.x = -Mathf.Sign(myRotY - camRotY);
                //mInput.x = Camera.main.transform.eulerAngles.z;

            }

        }
        else
        {
            // Update the input axes
            //base.Update();
            mInput.x = Input.GetAxis("Horizontal");
            mInput.y = Input.GetAxis("Vertical");

        }

        float time = Time.time;
		float delta = time - mLastInputSend;
		float delay = 1f / inputUpdates;

        //return;
        
        // Don't send updates more than 20 times per second
        if (delta > 1f) //0.05f)
		{
            

			// The closer we are to the desired send time, the smaller is the deviation required to send an update.
			float threshold = Mathf.Clamp01(delta - delay) * 0.5f;

			// If the deviation is significant enough, send the update to other players
			if (Tools.IsNotEqual(mLastInput.x, mInput.x, threshold) ||
				Tools.IsNotEqual(mLastInput.y, mInput.y, threshold))
			{
				mLastInputSend = time;
				mLastInput = mInput;
				tno.Send("SetAxis", Target.OthersSaved, mInput);
			}
		}

		// Since the input is sent frequently, rigidbody only needs to be corrected every couple of seconds.
		// Faster-paced games will require more frequent updates.
		if (mNextRB < time)
		{
			mNextRB = time + 1f / rigidbodyUpdates;
			tno.Send("SetRB", Target.OthersSaved, mRb.position, mRb.rotation, mRb.velocity, mRb.angularVelocity);
		}
	}

	/// <summary>
	/// RFC for the input will be called several times per second.
	/// </summary>

	[RFC]
	protected void SetAxis (Vector2 v) { mInput = v; }

	/// <summary>
	/// RFC for the rigidbody will be called once per second by default.
	/// </summary>

	[RFC]
	protected void SetRB (Vector3 pos, Quaternion rot, Vector3 vel, Vector3 angVel)
	{
		mRb.position = pos;
		mRb.rotation = rot;
		mRb.velocity = vel;
		mRb.angularVelocity = angVel;
	}
}
