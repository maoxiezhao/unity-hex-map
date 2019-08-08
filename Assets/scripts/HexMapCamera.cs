using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexMapCamera : MonoBehaviour {

    Transform swivel, stick; // 控制相机角和远近

    private float zoom = 1.0f;
    public float stickMinZoom;
    public float stickMaxZoom;

    public float swivelMaxZoom;
    public float swivelMinZoom;

    public float moveSpeedMinZoom;
    public float moveSppedMaxZoom;

    public float rotationSpeed;
    float rotationAngle;
    public HexGrid hexGrid;

    private void Awake()
    {
        swivel = transform.GetChild(0);
        stick = swivel.GetChild(0);
    }

    // Update is called once per frame
    void Update () {
        float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
        if (zoomDelta != 0.0f) {
            AdjustZoom(zoomDelta);
        }

        float rotationDelta = Input.GetAxis("Rotation");
        if (rotationDelta != 0.0f) {
            AdjustRotation(rotationDelta);
        }

        float xDelta = Input.GetAxis("Horizontal");
        float zDelta = Input.GetAxis("Vertical");
        if (xDelta != 0.0f || zDelta != 0.0f) {
            AdjustPosition(xDelta, zDelta);
        }
	}

    void AdjustZoom(float delta)
    {
        zoom = Mathf.Clamp01(zoom + delta);

        float distance = Mathf.Lerp(stickMinZoom, stickMaxZoom, zoom);
        stick.localPosition = new Vector3(0.0f, 0.0f, distance);

        float angle = Mathf.Lerp(swivelMinZoom, swivelMaxZoom, zoom);
        swivel.localRotation = Quaternion.Euler(angle, 0f, 0f);
    }

    void AdjustPosition(float xDalta, float zDelta)
    {
        Vector3 dir = transform.localRotation * new Vector3(xDalta, 0, zDelta).normalized;
        float damping = Mathf.Max(Mathf.Abs(xDalta), Mathf.Abs(zDelta));
        float distance = 
            Mathf.Lerp(moveSpeedMinZoom, moveSppedMaxZoom, zoom) * Time.deltaTime * damping;

        Vector3 pos = transform.localPosition;
        pos += dir * distance;
        transform.localPosition = ClampPosition(pos);
    }

    Vector3 ClampPosition(Vector3 pos)
    {
        float xMax = (hexGrid.chunkCountX * HexMetrics.chunkSizeX - 0.5f) * 
            (2f * HexMetrics.innerRadius);
        pos.x = Mathf.Clamp(pos.x, 0.0f, xMax);

        float zMax = (hexGrid.chunkCountZ * HexMetrics.chunkSizeZ - 1.0f) * 
            (1.5f * HexMetrics.outerRadius);
        pos.z = Mathf.Clamp(pos.z, 0.0f, zMax);

        return pos;
    }

    void AdjustRotation(float delta)
    {
        rotationAngle += delta * rotationSpeed * Time.deltaTime;
        if (rotationAngle < 0.0f){
            rotationAngle += 360.0f;
        }
        else if (rotationAngle >= 360.0f) {
            rotationAngle -= 360.0f;
        }

        transform.localRotation = Quaternion.Euler(0.0f, rotationAngle, 0.0f);
    }
}
