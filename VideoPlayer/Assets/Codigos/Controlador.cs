using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine.SceneManagement;

public class Controlador : MonoBehaviour {

    [DllImport("universal")]
    private static extern int FreeMountUsb();

        


    public static Controlador instancia;

    public Text txtCamino;
    public Text FechaHora;
    public Text ListaVacia;
    private string _s_ = "/";

    private int Nivel = 0;
    private List<Vector2> UltimaPos = new List<Vector2>();

    public GameObject PanelVideo;
    public Image PanelVideoImagen;
    public GameObject PanelImagen;
    public Image PanelImagenImagen;
        
    public GameObject carpetasPrefab;
    public GameObject ficherosPrefabMP4;
    public GameObject ficherosPrefabMOV;
    public GameObject ficherosPrefabSRT;
    public GameObject ficherosPrefabFOT;
    public ScrollRect scrollRect;
    public RectTransform contentPanel;
    public Slider miVolumen;
    public Text Subtitulos;

    private List<GameObject> ObjetosCreados = new List<GameObject>();
    private List<string> TODOS = new List<string>();

    private int Posicion = 0;
    private string[] scaneo;

    public string camino = "/usb0";
    private bool Paso = true;
    private bool EnImagen = false;
    
    public bool EnVideo = false;
    public bool FullScreen = false;
        
    public void Awake()
    {
        instancia = this;
    }
        
    Vector3 posOriginal;
    Vector2 sizeOriginal;
    Vector2 sizeOriginalVideoImagen;
    void Start()
    {
        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
        FechaHora.text = System.DateTime.Now.AddHours(-3).ToShortTimeString() + "\n" + System.DateTime.Now.AddHours(-3).ToLongDateString();
        StartCoroutine(ActualizarFechaHora());
                        
        try
        {
            FreeMountUsb();
        }
        catch { ;}

        CrearDirectorio();

        posOriginal = PanelVideo.transform.localPosition;
        sizeOriginal = PanelVideo.GetComponent<RectTransform>().sizeDelta;
    }

    IEnumerator ActualizarFechaHora()
    {
        while (true)
        {
            yield return new WaitForSeconds(60);
            FechaHora.text = System.DateTime.Now.AddHours(-3).ToShortTimeString() + "\n" + System.DateTime.Now.AddHours(-3).ToLongDateString();
        }
    }

    void CrearDirectorio()
    {
        txtCamino.text = camino.Replace("\\", "/");

        List<string> CarpetasCreadas = new List<string>();
        List<string> FicherosCreados = new List<string>();
        GameObject objeto = null;

        scaneo = Directory.GetFileSystemEntries(txtCamino.text);
        foreach (string registro in scaneo)
        {
            if (Directory.Exists(registro))
            {
                CarpetasCreadas.Add(registro);
            }
            else
            {
                FicherosCreados.Add(registro);
            }
        }

        for (int i = 0; i < CarpetasCreadas.Count; i++)
        {
            objeto = Instantiate(carpetasPrefab, transform);
            objeto.GetComponentInChildren<Text>().text = Path.GetFileName(CarpetasCreadas[i]);

            ObjetosCreados.Add(objeto);
            TODOS.Add(CarpetasCreadas[i]);
        }
        for (int i = 0; i < FicherosCreados.Count; i++)
        {
            switch (Path.GetExtension(FicherosCreados[i]).ToLower())
            {
                case ".mov":
                    objeto = Instantiate(ficherosPrefabMOV, transform);
                    objeto.GetComponentInChildren<Text>().text = Path.GetFileName(FicherosCreados[i]);
                    ObjetosCreados.Add(objeto);
                    TODOS.Add(FicherosCreados[i]);
                    break;
                case ".mp4":
                    objeto = Instantiate(ficherosPrefabMP4, transform);
                    objeto.GetComponentInChildren<Text>().text = Path.GetFileName(FicherosCreados[i]);
                    ObjetosCreados.Add(objeto);
                    TODOS.Add(FicherosCreados[i]);
                    break;
                case ".srt":
                case ".ssa":
                case ".ass":
                    objeto = Instantiate(ficherosPrefabSRT, transform);
                    objeto.GetComponentInChildren<Text>().text = Path.GetFileName(FicherosCreados[i]);
                    ObjetosCreados.Add(objeto);
                    TODOS.Add(FicherosCreados[i]);
                    break;
                case ".jpg":
                case ".png":
                    objeto = Instantiate(ficherosPrefabFOT, transform);
                    objeto.GetComponentInChildren<Text>().text = Path.GetFileName(FicherosCreados[i]);
                    ObjetosCreados.Add(objeto);
                    TODOS.Add(FicherosCreados[i]);
                    break;
            }
        }

        scrollRect.verticalNormalizedPosition = 1;
        
        // si hay algo selecionar el 1ro de la lista
        if (TODOS.Count > 0)
        {
            ObjetosCreados[0].transform.GetChild(0).gameObject.SetActive(true);
            camino = TODOS[0];

            ListaVacia.gameObject.SetActive(false);
        }
        else
        {
            camino = "";
        }

        Paso = true;
    }

    void Update()
    {
        if (!EnImagen && !FullScreen)
        {
            // movimientos arriba y abajo
            if ((Input.GetAxis("dpad1_vertical") > 0 || Input.GetAxis("leftstick1vertical") < 0 || Input.GetAxis("rightstick1vertical") < 0 || Input.GetKey(KeyCode.UpArrow)) && Posicion > 0 && Paso)
            {
                Paso = false;

                ObjetosCreados[Posicion].transform.GetChild(0).gameObject.SetActive(false);
                Posicion--;
                ObjetosCreados[Posicion].transform.GetChild(0).gameObject.SetActive(true);
                camino = TODOS[Posicion];

                if (contentPanel.anchoredPosition.y > 0)
                {
                    contentPanel.anchoredPosition -= new Vector2(0, 45);
                }
                if (contentPanel.anchoredPosition.y < 0)
                {
                    contentPanel.anchoredPosition = new Vector2(0, 0);
                }
                       
                StartCoroutine(SeguirPasando());
            }

            if ((Input.GetAxis("dpad1_vertical") < 0 || Input.GetAxis("leftstick1vertical") > 0 || Input.GetAxis("rightstick1vertical") > 0 || Input.GetKey(KeyCode.DownArrow)) && Posicion < TODOS.Count - 1 && Paso)
            {
                Paso = false;

                ObjetosCreados[Posicion].transform.GetChild(0).gameObject.SetActive(false);
                Posicion++;
                ObjetosCreados[Posicion].transform.GetChild(0).gameObject.SetActive(true);
                camino = TODOS[Posicion];

                if (scrollRect.verticalNormalizedPosition >= 0 && Posicion > 9)
                {
                    contentPanel.anchoredPosition += new Vector2(0, 45);
                }

                StartCoroutine(SeguirPasando());
            }

            // abrir o ejecutar
            if (Input.GetKeyDown(KeyCode.Joystick1Button0) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                try
                {
                    //LOG = "";
                    if (Directory.Exists(camino)) // si es una carpeta abrirla
                    {
                        Nivel++;
                        UltimaPos.Add(new Vector2(Posicion, contentPanel.anchoredPosition.y));

                        LimpiarTodo();
                        CrearDirectorio();
                    }
                    else // si no ejecutar el fichero si esta soportado
                    {
                        switch (Path.GetExtension(camino).ToLower())
                        {
                            case ".mov":
                            case ".mp4":
                                PlayVideo();
                                break;
                            case ".jpg":
                            case ".png":
                                MostrarImagen();
                                break;
                        }
                    }
                }
                catch // (System.Exception ex)
                {
                    ; //LOG = "Error " + ex.Message;
                }
            }

            // refrescar usb
            if (Input.GetKeyDown(KeyCode.Joystick1Button9))
            {
                SceneManager.LoadScene("main");
            }
        }

        // atras o cerrar opciones
        if (Input.GetKeyDown(KeyCode.Joystick1Button1) || Input.GetKeyDown(KeyCode.Keypad6))
        {
            if (FullScreen)
            {
                FullScreen = false;
                PanelVideo.transform.localPosition = posOriginal;
                PanelVideo.GetComponent<RectTransform>().sizeDelta = sizeOriginal;
                PanelVideoImagen.GetComponent<RectTransform>().sizeDelta = sizeOriginalVideoImagen;

                Subtitulos.fontSize = 32;
                Subtitulos.transform.localPosition = new Vector2(0, -200);

                return;
            }

            if (EnImagen)
            {
                //LOG = "";
                EnImagen = false;
                PanelImagen.gameObject.SetActive(false);
            }
            else
            {
                if (txtCamino.text != "/usb0")
                {
                    Nivel--;

                    //LOG = "";
                    LimpiarTodo();

                    camino = txtCamino.text.Substring(0, txtCamino.text.LastIndexOf(_s_));
                    
                    CrearDirectorio();

                    Posicion = (int)UltimaPos[Nivel].x;
                    contentPanel.anchoredPosition += new Vector2(0, UltimaPos[Nivel].y);
                    UltimaPos.RemoveAt(Nivel);

                    ObjetosCreados[0].transform.GetChild(0).gameObject.SetActive(false);
                    ObjetosCreados[Posicion].transform.GetChild(0).gameObject.SetActive(true);
                    camino = TODOS[Posicion];
                }
            }
        }

        // cambiar FullScreen y normal
        if ((Input.GetKeyDown(KeyCode.Joystick1Button3) || Input.GetKeyDown(KeyCode.Keypad8)) && Paso)
        {
            Paso = false;

            if (!FullScreen)
            {
                PanelVideo.transform.localPosition = Vector3.zero;
                PanelVideo.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 0);

                Subtitulos.fontSize = 64;
                Subtitulos.transform.localPosition = new Vector2(0, -430);

                // tamaño de la Image del Video
                sizeOriginalVideoImagen = PanelVideoImagen.gameObject.GetComponent<RectTransform>().sizeDelta;

                int DW = 1920;
                int DH = 1080;

                float AR = (float)PanelVideoImagen.gameObject.GetComponent<RectTransform>().rect.width / (float)PanelVideoImagen.gameObject.GetComponent<RectTransform>().rect.height;
                float XX = DW;
                float YY = DW / AR;

                if (YY > DH)
                {
                    YY = DH;
                    XX = YY * AR;
                }

                PanelVideoImagen.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(Mathf.RoundToInt(XX), Mathf.RoundToInt(YY));                
            }
            else
            {
                PanelVideo.transform.localPosition = posOriginal;
                PanelVideo.GetComponent<RectTransform>().sizeDelta = sizeOriginal;
                PanelVideoImagen.GetComponent<RectTransform>().sizeDelta = sizeOriginalVideoImagen;

                Subtitulos.fontSize = 32;
                Subtitulos.transform.localPosition = new Vector2(0, -200);
            }

            FullScreen = !FullScreen;
            StartCoroutine(SeguirPasando());
        }
                
        //txtLog.text = LOG;
    }

    public void ParoEnFullScreen()
    {
        FullScreen = false;
        PanelVideo.transform.localPosition = posOriginal;
        PanelVideoImagen.GetComponent<RectTransform>().sizeDelta = sizeOriginalVideoImagen;
        PanelVideo.GetComponent<RectTransform>().sizeDelta = sizeOriginal;
    }

    private void LimpiarTodo()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        ObjetosCreados.Clear();
        TODOS.Clear();
        Posicion = 0;
    }

    private IEnumerator SeguirPasando()
    {
        if (Input.GetAxis("joystick1_left_trigger") != 0)
        {
            yield return null;
        }
        else
        {
            yield return new WaitForSeconds(0.15f);
        }
        
        Paso = true;
    }

    private void MostrarImagen()
    {
        EnImagen = true;

        byte[] bytes = File.ReadAllBytes(camino);
        Texture2D texture = new Texture2D(0, 0, TextureFormat.RGB24, false);
        texture.filterMode = FilterMode.Trilinear;
        texture.LoadImage(bytes);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.0f, 0.0f), 1.0f);

        float AR = 0;
        float XX = 0;
        float YY = 0;
        if (texture.height >= 1080)
        {
            AR = (float)texture.width / (float)texture.height;
            YY = Mathf.Min(texture.width, 1080);
            XX = YY * AR;

            if (XX > 1920)
            {
                XX = 1920;
                YY = 1920 / AR;
            }
        }
        else
        {
            AR = (float)texture.width / (float)texture.height;
            XX = Mathf.Min(texture.width, 1920);
            YY = XX / AR;
        }

        PanelImagenImagen.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(XX, Mathf.RoundToInt(YY));
        PanelImagen.gameObject.SetActive(true);
        PanelImagenImagen.sprite = sprite;
    }

    private void PlayVideo()
    {
        EnVideo = true;
    }

    public void UpdateVolumen(float volumen)
    {
        miVolumen.value = volumen;
    }
}