using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    public Transform target;


	private Vector3 desiredBob;

	private Vector3 bobOffset;

	public float bobMultiplier = 0.4f;

	private float desiredTilt;

	private float tilt;

	[HideInInspector]
	public Vector3 vaultOffset;
	[HideInInspector]
	public Vector3 desyncOffset;

	public Camera mainCam;

	public static MoveCamera Instance { get; private set; }

    private void Awake()
    {
		Instance = this;
    }

    private void LateUpdate()
    {
		UpdateBob();
		transform.position = target.position + vaultOffset + bobOffset + desyncOffset;
		vaultOffset = Vector3.Slerp(vaultOffset, Vector3.zero, Time.deltaTime * 7f);
		desyncOffset = Vector3.Lerp(desyncOffset, Vector3.zero, Time.deltaTime * 15f);
		if (PlayerMovement.Instance.IsCrouching())
		{
			desiredTilt = 6f;
		}
		else
		{
			desiredTilt = 0f;
		}
		tilt = Mathf.Lerp(tilt, desiredTilt, Time.deltaTime * 8f);
		Vector3 eulerAngles = base.transform.rotation.eulerAngles;
		eulerAngles.z = tilt/2f;
		base.transform.rotation = Quaternion.Euler(eulerAngles);
	}

	public void BobOnce(Vector3 bobDirection)
	{
		Vector3 vector = ClampVector(bobDirection * 0.15f, -3f, 3f);
		desiredBob = vector * bobMultiplier;
	}

	private Vector3 ClampVector(Vector3 vec, float min, float max)
	{
		return new Vector3(Mathf.Clamp(vec.x, min, max), Mathf.Clamp(vec.y, min, max), Mathf.Clamp(vec.z, min, max));
	}

	private void UpdateBob()
	{
		desiredBob = Vector3.Lerp(desiredBob, Vector3.zero, Time.deltaTime * 15f * 0.5f);
		bobOffset = Vector3.Lerp(bobOffset, desiredBob, Time.deltaTime * 15f);
	}
}
