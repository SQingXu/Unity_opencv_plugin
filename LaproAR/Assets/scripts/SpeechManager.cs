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
            origPos = GameObject.Find("Origin").GetComponent<OriginPosition>();
            if(origPos == null)
            {
                Debug.Log("No OriginPosition Instance found");
            }
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
           
            keywordRecognizer = new KeywordRecognizer(keywords.Keys.ToArray());
            keywordRecognizer.OnPhraseRecognized += Keyword_OnRecognized;
            keywordRecognizer.Start();
            Debug.Log("Speechmanager Started");
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
