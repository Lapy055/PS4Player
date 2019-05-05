using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;

using UnityEngine.PS4;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class PS4VideoPlaybackSample : MonoBehaviour
{
    public string moviePath = "";

    public Image videoImage;
    public Slider timelineSlider;
    public Text timelineCurrentDisplay;
    public Text timelineTotalDisplay;
    public Text Subtitulos;
    
    PS4VideoPlayer video;
    PS4ImageStream lumaTex;
    PS4ImageStream chromaTex;
    public float Volumen = 1;
    PS4VideoPlayer.Looping isLooping = PS4VideoPlayer.Looping.None;
    bool Paso = true;

    List<SubtitlesParser.Classes.SubtitleItem> itemsubs = null;
    int linea = 0;

    void Start()
    {
        // In 5.3 this event is triggered automatically, as an optimization in 5.4 onwards you need to register the callback
        PS4VideoPlayer.OnMovieEvent += OnMovieEvent;

        video = new PS4VideoPlayer(); // This sets up a VideoDecoderType.DEFAULT system
        video.PerformanceLevel = PS4VideoPlayer.Performance.Optimal;
        video.demuxVideoBufferSize = 8 * 1024 * 1024; // Change the demux buffer from it's 1mb default
        video.numOutputVideoFrameBuffers = 8; // Increasing this can stop frame stuttering
        
        lumaTex = new PS4ImageStream();
        lumaTex.Create(1920, 1080, PS4ImageStream.Type.R8, 0);
        chromaTex = new PS4ImageStream();
        chromaTex.Create(1920 / 2, 1080 / 2, PS4ImageStream.Type.R8G8, 0);
        video.Init(lumaTex, chromaTex);
        
        // Apply video textures to the UI image
        videoImage.material.SetTexture("_MainTex", lumaTex.GetTexture());
        videoImage.material.SetTexture("_CromaTex", chromaTex.GetTexture());
    }

    public void OnEnable()
    {
        Paso = true;
    }

    void Update()
    {
        // Required to keep the video processing
        video.Update();

        // play/pause
        if (Controlador.instancia.EnVideo)
        {
            Controlador.instancia.EnVideo = false;

            if (moviePath != Controlador.instancia.camino)
            {
                VideoStop();
                moviePath = Controlador.instancia.camino;
            }

            VideoPlayPause();
        }

        if (Controlador.instancia.FullScreen && Input.GetKeyDown(KeyCode.Joystick1Button0) && Paso)
        {
            Paso = false;

            VideoPlayPause();

            StartCoroutine(SeguirPasando());
        }

        // stop video
        if (Input.GetKeyDown(KeyCode.Joystick1Button2))
        {
            VideoStop();

            if (Controlador.instancia.FullScreen)
            {
                Controlador.instancia.ParoEnFullScreen();
            }
        }

        // rewind and fastforward
        if (video.playerState == PS4VideoPlayer.VidState.PLAY || video.playerState == PS4VideoPlayer.VidState.PAUSE)
        {
            if (Input.GetAxis("dpad1_horizontal") < 0 && Paso)
            {
                Paso = false;

                VideoRewind();

                StartCoroutine(SeguirPasando());
            }

            if (Input.GetAxis("dpad1_horizontal") > 0 && Paso)
            {
                Paso = false;

                VideoFastForward();

                StartCoroutine(SeguirPasando());
            }
        }

        // volumen
        if ((Input.GetKey(KeyCode.Joystick1Button4) || Input.GetKey(KeyCode.A)) && Paso)
        {
            Paso = false;

            Volumen -= 0.1f;
            if (Volumen < 0) Volumen = 0;

            video.SetVolume(Volumen);
            Controlador.instancia.UpdateVolumen(Volumen);

            StartCoroutine(SeguirPasando());
        }

        if ((Input.GetKey(KeyCode.Joystick1Button5) || Input.GetKey(KeyCode.D)) && Paso)
        {
            Paso = false;

            Volumen += 0.1f;
            if (Volumen > 1) Volumen = 1;
            
            video.SetVolume(Volumen);
            Controlador.instancia.UpdateVolumen(Volumen);

            StartCoroutine(SeguirPasando());
        }

        if(video.playerState > PS4VideoPlayer.VidState.READY)
        {
            // The video CurrentTime and Length will return 0 if the video player is not in a valid active state
            DateTime videoCurrentTime = new DateTime(video.CurrentTime * 10000);
            DateTime videoLength = new DateTime(video.Length * 10000);

            // Display the current time on a slider bar
            timelineSlider.value = videoCurrentTime.Ticks;
            timelineSlider.maxValue = videoLength.Ticks;

            // Display the current time and the total time in text form
            timelineCurrentDisplay.text = videoCurrentTime.ToString("HH:mm:ss");
            timelineTotalDisplay.text = videoLength.ToString("HH:mm:ss");

            // subtitulos
            if (itemsubs != null && video.playerState == PS4VideoPlayer.VidState.PLAY)
            {
                int tiempoActual = Convert.ToInt32(videoCurrentTime.ToString("HH:mm:ss:fff").Replace(":", ""));

                if (itemsubs[linea].StartTime <= tiempoActual && itemsubs[linea].EndTime >= tiempoActual)
                {
                    if (itemsubs[linea].Lines != null)
                    {
                        Subtitulos.text = itemsubs[linea].Lines.Trim();
                    }

                    linea++;
                }
                else if (linea > 0 && itemsubs[linea - 1].EndTime <= tiempoActual)
                {
                    Subtitulos.text = "";
                }
            }
        }       
        
        CropVideo();
    }

    void CropVideo()
    {
        // The video player on the PS4 frequently generates video on larger textures than the video, and requires us to crop the video. This code calculates
        // the crop values and passes the data on to the TRANSFORM_TEX call in the shader, without it we get nasty black borders at the edge of video
        if (videoImage != null)
        {
            int cropleft, cropright, croptop, cropbottom, width, height;
            video.GetVideoCropValues(out cropleft, out cropright, out croptop, out cropbottom, out width, out height);
            float scalex = 1.0f;
            float scaley = 1.0f;
            float offx = 0.0f;
            float offy = 0.0f;

            if ((width > 0) && (height > 0))
            {
                int fullwidth = width + cropleft + cropright;
                scalex = (float)width / (float)fullwidth;
                offx = (float)cropleft / (float)fullwidth;
                int fullheight = height + croptop + cropbottom;
                scaley = (float)height / (float)fullheight;
                offy = (float)croptop / (float)fullheight;
            }

            // Typically we want to invert the Y on the video because thats how planes UV's are layed out
            videoImage.material.SetVector("_MainTex_ST", new Vector4(scalex, scaley * -1, offx, 1 - offy));
        }
    }

    public void VideoPlayPause()
    {
        // Pause if playing, Resume if paused, Play if stopped
        if (video.playerState == PS4VideoPlayer.VidState.PLAY)
        {
            video.Pause();
        }
        else if(video.playerState == PS4VideoPlayer.VidState.PAUSE)
        {
            video.Resume();
            //playPauseIcon.sprite = pauseIcon;
        }
        else
        {            
            try
            {
                video.Play(moviePath, isLooping);

                StopAllCoroutines();
                StartCoroutine(CheckDimensions(moviePath));

                // cargar subtitulos si existen y si se llaman igual al video
                string caminoSub = moviePath.Replace(Path.GetExtension(moviePath), "");
                if (File.Exists(caminoSub + ".srt"))
                {
                    SubtitlesParser.Classes.Parsers.SrtParser parser = new SubtitlesParser.Classes.Parsers.SrtParser();

                    Subtitulos.text = "";
                    linea = 0;
                    itemsubs = parser.ParseStream(caminoSub + ".srt");
                }
                else if (File.Exists(caminoSub + ".ssa"))
                {
                    SubtitlesParser.Classes.Parsers.SsaParser parser = new SubtitlesParser.Classes.Parsers.SsaParser();

                    Subtitulos.text = "";
                    linea = 0;
                    itemsubs = parser.ParseStream(caminoSub + ".ssa");
                }
                else if (File.Exists(caminoSub + ".ass"))
                {
                    SubtitlesParser.Classes.Parsers.SsaParser parser = new SubtitlesParser.Classes.Parsers.SsaParser();

                    Subtitulos.text = "";
                    linea = 0;
                    itemsubs = parser.ParseStream(caminoSub + ".ass");
                }
                else
                {
                    linea = 0;
                    itemsubs = null;
                }
            }
            catch (Exception ex)
            {
                linea = 0;
                itemsubs = null;
                Subtitulos.text = "Error: " + ex.Message;
            }
        }

        // Calling Play ignores the current mute settings. This reapplies them
        video.SetVolume(Volumen);
    }

    // Stop playback and reset the current time displays to zero
    public void VideoStop()
    {
        video.Stop();
        //playPauseIcon.sprite = playIcon;
        timelineCurrentDisplay.text = "00:00:00";
        timelineSlider.value = 0;

        Subtitulos.text = "";
        linea = 0;
        itemsubs = null;
    }

    // Jump forwards 1000ms
    public void VideoFastForward()
    {
        int Milisegundo = 0;
        if (Input.GetAxis("joystick1_left_trigger") != 0)
            Milisegundo = 60000;
        else
            Milisegundo = 10000;

        if (video.playerState == PS4VideoPlayer.VidState.PLAY)
        {
            video.Pause();
            video.JumpToTime(video.GetCurrentTime() + (ulong)Milisegundo);
            video.Resume();

            video.Pause();
            SincronizarSubtitulo();
            video.Resume();
        }
        else
        {
            video.JumpToTime(video.GetCurrentTime() + (ulong)Milisegundo);
            SincronizarSubtitulo();
        }
    }

    // Jump backwards 1000ms
    public void VideoRewind()
    {
        int Milisegundo = 0;
        if (Input.GetAxis("joystick1_left_trigger") != 0)
            Milisegundo = 60000;
        else
            Milisegundo = 10000;

        if (video.playerState == PS4VideoPlayer.VidState.PLAY)
        {
            video.Pause();
            video.JumpToTime(video.GetCurrentTime() - (ulong)Milisegundo);
            video.Resume();

            video.Pause();
            SincronizarSubtitulo();
            video.Resume();
        }
        else
        {
            video.JumpToTime(video.GetCurrentTime() - (ulong)Milisegundo);
            SincronizarSubtitulo();
        }
    }

    //Sincronizar Subtitulo cuando haya un Rewing o Fastforward
    private void SincronizarSubtitulo()
    {
        if (itemsubs != null)
        {
            Subtitulos.text = "";

            DateTime videoCurrentTime = new DateTime(video.CurrentTime * 10000);
            int tiempoActual = Convert.ToInt32(videoCurrentTime.ToString("HH:mm:ss:fff").Replace(":", ""));           

            for (int i = 0; i < itemsubs.Count; i++)
            {
                if (itemsubs[i].StartTime > tiempoActual)
                {
                    linea = i;

                    if (i > 0 && itemsubs[i - 1].EndTime < tiempoActual)
                    {
                        if (itemsubs[i - 1].Lines != null)
                        {
                            Subtitulos.text = itemsubs[i - 1].Lines.Trim();
                        }
                    }

                    return;
                }
            }
        }
    }

    // Toggle looping. Note that this only takes effect after starting playback, and doesn't affect a currently playing video
    public void VideoToggleLooping()
    {
        if(isLooping == PS4VideoPlayer.Looping.None)
        {
            isLooping = PS4VideoPlayer.Looping.Continuous;
            //loopingIcon.color = Color.white;
        }
        else
        {
            isLooping = PS4VideoPlayer.Looping.None;
            //loopingIcon.color = new Color(1, 1, 1, 0.25f);
        }
    }

    void OnMovieEvent(int FMVevent)
    {
        ;
    }

    private IEnumerator SeguirPasando()
    {
        yield return new WaitForSeconds(0.2f);
        Paso = true;
    }

    float UAR = (float)874 / (float)492;
    IEnumerator CheckDimensions(string url)
    {
        while (video.playerState != PS4VideoPlayer.VidState.PLAY)
            yield return null;
        
        try
        {
            GameObject tempVideo = new GameObject("Tempvideo");
            UnityEngine.Video.VideoPlayer videoPlayer = tempVideo.AddComponent<UnityEngine.Video.VideoPlayer>();
            videoPlayer.renderMode = UnityEngine.Video.VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = new RenderTexture(1, 1, 0);
            videoPlayer.source = UnityEngine.Video.VideoSource.Url;
            videoPlayer.url = url;
            videoPlayer.prepareCompleted += (UnityEngine.Video.VideoPlayer source) =>
            {
                float AR = (float)source.texture.width / (float)source.texture.height;

                if (AR != UAR)
                {
                    UAR = AR;
                    int DW = 874;
                    int DH = 492;
                    float XX = DW;
                    float YY = DW / AR;

                    if (YY > DH)
                    {
                        YY = DH;
                        XX = YY * AR;
                    }

                    videoImage.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(Mathf.RoundToInt(XX), Mathf.RoundToInt(YY));
                }

                Destroy(tempVideo);
            };
            videoPlayer.Prepare();
        }
        catch
        {
            videoImage.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(874, 492);
        }
    }
}
