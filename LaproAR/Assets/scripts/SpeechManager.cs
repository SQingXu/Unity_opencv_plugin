using UnityEngine;
using UnityEngine.Windows.Speech;
using System.Collections.Generic;
using System.Linq;
namespace PosterAlignment
{
    public class SpeechManager : MonoBehaviour
    {

        KeywordRecognizer keywordRecognizer = null;
        OriginPosition origPos = null;
        TestDLL MarkerController = null;
        Dictionary<string, System.Action> keywords = new Dictionary<string, System.Action>();

        // Use this for initialization
        private void Awake()
        {
            if (!HoloHelper.isHololens())
            {
                this.enabled = false;
                Debug.Log("Not Holo");
            }
        }
        void Start()
        {
#if !UNITY_EDITOR
            origPos = GameObject.Find("Origin").GetComponent<OriginPosition>();
            if(origPos == null)
            {
                Debug.Log("No OriginPosition Instance found");
            }
            //MarkerController = GameObject.Find("marker").GetComponent<TestDLL>();
            //keywords.Add("Reference Calibration", () =>
            // {
            //     Debug.Log("Voice recognized");
            //     if (!ReferenceCalibration.Instance.isCalibrating())
            //     {
            //         ReferenceCalibration.Instance.SendMessage("OnStartReferenceCalibration");
            //     }
            // });
            keywords.Add("Stable", () =>
            {
                Debug.Log("stable command recognized");
                if (origPos != null)
                {
                    origPos.stable = true;
                    origPos.addAnchor();

                }
                //if (MarkerController != null)
                //{
                //    MarkerController.StableObject();
                //}
            });
            keywords.Add("Follow", () =>
            {
                Debug.Log("follow command recognized");
                if (origPos != null)
                {
                    origPos.removeAnchor();
                    origPos.stable = false;
                }
            });
            keywords.Add("Track", () =>
            {
                Debug.Log("track command recognized");
                if (MarkerController != null)
                {
                    MarkerController.TrackObject();
                }
            });
           
            keywordRecognizer = new KeywordRecognizer(keywords.Keys.ToArray());
            keywordRecognizer.OnPhraseRecognized += Keyword_OnRecognized;
            keywordRecognizer.Start();
            Debug.Log("Speechmanager Started");
#else
#endif
        }

        void Keyword_OnRecognized(PhraseRecognizedEventArgs arg)
        {
            System.Action keywordAction;
            if (keywords.TryGetValue(arg.text, out keywordAction))
            {
                keywordAction.Invoke();
            }

        }
    }
}
