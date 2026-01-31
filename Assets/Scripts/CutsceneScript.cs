using UnityEngine;
using UnityEngine.Video;

public class CutsceneScript : MonoBehaviour
{
    VideoPlayer cutScene;

    // Update is called once per frame
    void Update()
    {
        cutScene.loopPointReached += OnVideoEnd;

        void OnVideoEnd(VideoPlayer vp)
        {
            
        }
    }
}
