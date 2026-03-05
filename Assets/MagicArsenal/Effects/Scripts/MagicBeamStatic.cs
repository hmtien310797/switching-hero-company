using System;
using UnityEngine;

namespace MagicArsenal
{
    public class MagicBeamStatic : MonoBehaviour
    {
//         public bool IsRunningItSelf;
//         public string BeamStartKey;
//         public string BeamEndKey;
//         public string BeamLineRendererKey;
//
//         private GameObject beamStart;
//         private GameObject beamEnd;
//         private GameObject beam;
//         private LineRenderer line;
//
//         [Header("Beam Options")]
//         public bool alwaysOn = true; //Enable this to spawn the beam when script is loaded.
//         public bool beamCollides = true; //Beam stops at colliders
//         public bool HasFinalDestination = false;
//
//         [ShowIf(nameof(HasFinalDestination), false)]
//         public float beamLength = 100; //Ingame beam length
//
//         //How far from the raycast hit point the end effect is positioned
//         public float beamEndOffset = 0f;
//
//         //How fast the texture scrolls along the beam, can be negative or positive.
//         public float textureScrollSpeed = 0f;
//
//         //Set this to the horizontal length of your texture relative to the vertical. 
//         public float textureLengthScale = 1f;
//
//         //Example: if texture is 200 pixels in height and 600 in length, set this to 3
//
//         private Vector3 finalDestination = Vector3.zero;
//
//         private void OnEnable()
//         {
//             if (IsRunningItSelf && BattleSystem.Instance.StageSystem.GameStarted)
//             {
//                 SpawnBeam(null, 0);
//             }
//         }
//
//         public void SpawnBeam(Transform targetTransform, float timeSpan) //This function spawns the prefab with linerenderer
//         {
//             if (targetTransform != null)
//             {
//                 finalDestination = targetTransform.position;
//             }
//
//             var BeamStartObject = GenericAddressablePool.Instance.GetObject<ParticleSystem>(BeamStartKey);
//             var BeamEndObject = GenericAddressablePool.Instance.GetObject<ParticleSystem>(BeamEndKey);
//             var BeamObject = GenericAddressablePool.Instance.GetObject<LineRenderer>(BeamLineRendererKey);
//
//             BeamStartObject.transform.position = transform.position;
//             BeamEndObject.transform.position = transform.position;
//             BeamObject.transform.position = transform.position;
//
//             BeamStartObject.transform.parent = transform;
//             BeamEndObject.transform.parent = transform;
//             BeamObject.transform.parent = transform;
//
//             BeamObject.transform.rotation = transform.rotation;
//
//             BeamStartObject.gameObject.SetActive(true);
//             BeamEndObject.gameObject.SetActive(true);
//             BeamObject.gameObject.SetActive(true);
//
//             beamStart = BeamStartObject.gameObject;
//             beamEnd = BeamEndObject.gameObject;
//             beam = BeamObject.gameObject;
//
//             line = beam.GetComponent<LineRenderer>();
//             line.useWorldSpace = true;
// #if UNITY_5_5_OR_NEWER
//             line.positionCount = 2;
// #else
// 			line.SetVertexCount(2);
// #endif
//
//             line.SetPosition(0, transform.position);
//
//             Vector3 end;
//             RaycastHit hit;
//             if (beamCollides
//                 && Physics.Raycast(transform.position, transform.forward,
//                     out hit)) //Checks for collision
//                 end = hit.point - (transform.forward * beamEndOffset);
//             else
//                 end = transform.position + (transform.forward * beamLength);
//
//             end = HasFinalDestination ? finalDestination : end;
//             line.SetPosition(1, end);
//
//             if (beamStart)
//             {
//                 beamStart.transform.position = transform.position;
//                 beamStart.transform.LookAt(end);
//             }
//
//             if (beamEnd)
//             {
//                 beamEnd.transform.position = end;
//                 beamEnd.transform.LookAt(beamStart.transform.position);
//             }
//
//             float distance = Vector3.Distance(transform.position, end);
//             line.material.mainTextureScale =
//                 new Vector2(distance / textureLengthScale,
//                     1); //This sets the scale of the texture so it doesn't look stretched
//
//             line.material.mainTextureOffset -= new Vector2(Time.deltaTime * textureScrollSpeed, 0);
//
//             if (IsRunningItSelf)
//             {
//                 return;
//             }
//
//             GenericAddressablePool.Instance.Return(BeamStartObject.gameObject, timeSpan);
//             GenericAddressablePool.Instance.Return(BeamEndObject.gameObject, timeSpan);
//             GenericAddressablePool.Instance.Return(BeamObject.gameObject, timeSpan);
//             GenericAddressablePool.Instance.Return(gameObject, timeSpan);
//         }
//
//         private void OnDisable()
//         {
//             finalDestination = Vector3.zero;
//             if (!IsRunningItSelf)
//             {
//                 return;
//             }
//         }
//     }
    }
}
