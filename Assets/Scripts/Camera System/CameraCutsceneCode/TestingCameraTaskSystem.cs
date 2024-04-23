using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class TestingCameraTaskSystem : GameplayCutsceneBase
{
    public Transform newKeyframe;
    public AnimationCurve cameraSpringCurve;
    [SerializeField] private float panTime;
    [SerializeField] private float stayAtIvalTime;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(3);
        List<IEnumerator> cameraTasks = new List<IEnumerator>();
        Vector3 startPos = Helpers.MainCamera.transform.position;
        Quaternion startRot = Helpers.MainCamera.transform.rotation;

        cameraTasks.Add(LerpTransform(startPos, startRot, newKeyframe, panTime, cameraSpringCurve));
        cameraTasks.Add(new WaitForSecondsRealtime(stayAtIvalTime));
        cameraTasks.Add(LerpTransform(newKeyframe.position, newKeyframe.rotation, GetOriginalParent(), panTime, cameraSpringCurve));

        yield return ExecuteCameraTasks(cameraTasks);
    }
}
