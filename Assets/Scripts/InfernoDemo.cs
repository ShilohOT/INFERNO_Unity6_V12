using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Prototipo independiente: novela visual + afinidad + una batalla por turnos.
public class InfernoDemo : MonoBehaviour
{
    enum Phase { MainMenu, Options, Intro, ExploreHeaven, Parchment, ExplorePalace, Palace, Choice, ChoiceReply, Battle, Loot, Ending, Summary, Defeat }
    enum Emotion { Furia, Culpa, Inestabilidad }
    struct Line
    {
        public string speaker, message, expression;
        public Line(string s, string m, string e = "normal") { speaker = s; message = m; expression = e; }
    }

    readonly List<Line> intro = new List<Line> {
        new Line("PROTAGONISTA", "El Consejo Superior me ha encomendado una misión suicida... Soy el primer ángel enviado directamente al Infierno para negociar una tregua.", "triste"),
        new Line("PROTAGONISTA", "¿Por qué yo? Ni siquiera pertenezco a una de las primeras tríadas...", "triste"),
        new Line("PROTAGONISTA", "Si fracaso, podría desencadenar una guerra entre reinos.", "nerviosa"),
        new Line("PROTAGONISTA", "...", "triste"),
        new Line("PROTAGONISTA", "¡No puedo decepcionar a todo el reino angelical! Puedo hacerlo.", "decidida"),
        new Line("SISTEMA", "Un pergamino del Consejo Superior aparece flotando frente a ti.")
    };
    readonly List<Line> palace = new List<Line> {
        new Line("PROTAGONISTA", "Solo debo entregar un mensaje y podré irme...", "nerviosa"),
        new Line("PRÍNCIPE", "¿El Cielo envía a una simple plumífera de los bajos coros para negociar? Ni siquiera eres un ángel de la primera tríada.", "furioso"),
        new Line("ERUDITO", "Señor, es solo una mensajera, no un arcángel exterminador."),
        new Line("PRÍNCIPE", "Esto es un insulto. ¿Acaso creen que olvidaremos siglos de conflicto con unas cuantas palabras bonitas?", "furioso")
    };
    List<Line> ending;
    Phase phase;
    int step, princeAffinity = 40, scholarAffinity = 40;
    int choiceIndex = -1;
    bool peaceVictory;
    bool skillsExpanded, itemsExpanded;
    int totalGold, lastGoldLoot;
    string lastLootItem = "";
    Sprite fineOrnament, goldCoin;
    Sprite v10Menu, v10MenuRed, v10Dialogue, v10Panel, v10HUDHero, v10HUDEnemy, v10Header, v10FaceHero, v10FaceEnemy;
    Phase returnPhase = Phase.MainMenu;
    float musicVolume = 0.65f, effectsVolume = 0.8f;
    bool fullscreen = true;
    AudioSource musicSource, effectsSource;
    AudioClip menuMusic, heavenMusic, battleMusic;

    int heroHP, enemyHP, hostility, focus, determination, guard, exposedTurns, skillCooldown, turn;
    Emotion enemyEmotion = Emotion.Furia;
    string battleLog = "";

    Canvas canvas;
    RectTransform stage;
    Font font;
    Font headingFont;
    Sprite parchmentLight, ornamentalFrame;
    float battleImpactUntil, enemyAttackUntil;
    int lastHeroHP, lastEnemyHP;
    Sprite bgHeaven, bgPalace, bgPalaceDialogue, bgVictory, heroArt, princeArt, scholarArt, demonArt, skyWalk, palaceWalk;
    Sprite menuBackground, menuButtonNormal, menuButtonHover;
    Sprite[] angelFrames = new Sprite[4];
    Sprite[] princeIdleFrames = new Sprite[4];
    Sprite[] scholarIdleFrames = new Sprite[2];
    Sprite[] scrollIdleFrames = new Sprite[2];
    Sprite[] demonIdleFrames = new Sprite[4], demonAttackFrames = new Sprite[4], demonDeathFrames = new Sprite[4];
    // Animaciones del nuevo spritesheet: se cargan desde Resources, sin depender de Animator.
    readonly Dictionary<string, Sprite[]> heroAnimations = new Dictionary<string, Sprite[]>();
    readonly Dictionary<string, Sprite> dialoguePortraits = new Dictionary<string, Sprite>();
    string heroDirection = "Right";
    float battleAttackUntil;
    Sprite attackBeam;
    RectTransform walker; Image walkerImage; Text movementHint; Image princeExploreImage;
    float walkX = .16f, walkY = .345f, walkAnimation;
    bool moving;
    readonly Color ffBlue = new Color32(13, 21, 43, 247); // azul noche opaco para lectura
    readonly Color ffBlue2 = new Color32(31, 46, 80, 253); // botón contrastado
    readonly Color ivory = new Color32(250, 245, 228, 255); // marfil legible
    readonly Color gold = new Color32(240, 201, 120, 255); // borde oro cálido
    readonly Color ink = new Color32(32, 28, 26, 255);
    readonly Color blue = new Color32(92, 197, 232, 255);
    readonly Color deepBlue = new Color32(45, 92, 169, 255);
    readonly Color purple = new Color32(179, 115, 216, 255);
    readonly Color red = new Color32(226, 78, 99, 255);
    readonly Color green = new Color32(137, 207, 91, 255);

    void Awake()
    {
        // Tipografía más limpia y retro para lectura, con aire similar a VN / retro RPG.
        font = Font.CreateDynamicFontFromOSFont(new string[] { "Trebuchet MS", "Verdana", "Tahoma", "Arial", "Comic Sans MS" }, 22);
        headingFont = Font.CreateDynamicFontFromOSFont(new string[] { "Georgia", "Palatino Linotype", "Book Antiqua", "Garamond", "Times New Roman" }, 30);
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (headingFont == null) headingFont = font;
        parchmentLight = Resources.Load<Sprite>("Art/UI/pergamino_luz_referencia");
        ornamentalFrame = Resources.Load<Sprite>("Art/UI/marco_ornamental_referencia");
        fineOrnament = Resources.Load<Sprite>("Art/UI/marco_lineas_transparente");
        goldCoin = Resources.Load<Sprite>("Art/UI/coin_gold");
        v10Menu = Resources.Load<Sprite>("Art/UI/V10/menu_black");
        v10MenuRed = Resources.Load<Sprite>("Art/UI/V10/menu_red");
        v10Dialogue = Resources.Load<Sprite>("Art/UI/V10/dialogue");
        v10Panel = Resources.Load<Sprite>("Art/UI/V10/panel_black");
        v10HUDHero = Resources.Load<Sprite>("Art/UI/V10/hud_hero");
        v10HUDEnemy = Resources.Load<Sprite>("Art/UI/V10/hud_enemy");
        v10Header = Resources.Load<Sprite>("Art/UI/V10/header");
        v10FaceHero = Resources.Load<Sprite>("Art/UI/V10/face_hero");
        v10FaceEnemy = Resources.Load<Sprite>("Art/UI/V10/face_enemy");
        menuBackground = Resources.Load<Sprite>("Art/menu_pixel_background");
        menuButtonNormal = Resources.Load<Sprite>("Art/menu_button_normal");
        menuButtonHover = Resources.Load<Sprite>("Art/menu_button_hover");
        bgHeaven = Resources.Load<Sprite>("Art/cielo");
        bgPalace = Resources.Load<Sprite>("Art/palacio");
        bgPalaceDialogue = Resources.Load<Sprite>("Art/palacio_dialogo_principe");
        bgVictory = Resources.Load<Sprite>("Art/victoria_fondo_v12");
        heroArt = Resources.Load<Sprite>("Art/angel");
        princeArt = Resources.Load<Sprite>("Art/principe");
        scholarArt = Resources.Load<Sprite>("Art/erudito");
        demonArt = Resources.Load<Sprite>("Art/demonio");
        skyWalk = Resources.Load<Sprite>("Art/cielo_explore_v12");
        if (skyWalk == null) skyWalk = Resources.Load<Sprite>("Art/cielo_explore");
        palaceWalk = Resources.Load<Sprite>("Art/palacio_explore");
        for (int i=0;i<4;i++) {
            angelFrames[i] = Resources.Load<Sprite>("Art/angel_user_0_" + i);
        }
        for (int i=0;i<4;i++) princeIdleFrames[i] = Resources.Load<Sprite>("Art/PrinceIdleV12/prince_idle_" + i.ToString("00"));
        if (princeIdleFrames[0] == null)
            for (int i=0;i<2;i++) princeIdleFrames[i] = Resources.Load<Sprite>("Art/PrinceIdle/prince_idle_" + i.ToString("00"));
        for (int i=0;i<2;i++) scholarIdleFrames[i] = Resources.Load<Sprite>("Art/ScholarIdleV12/scholar_idle_" + i.ToString("00"));
        for (int i=0;i<2;i++) scrollIdleFrames[i] = Resources.Load<Sprite>("Art/ScrollIdleV12/scroll_idle_" + i.ToString("00"));
        LoadNewHeroAnimations();
        LoadNewDemonAnimations();
        LoadDialoguePortraits();
        // Conservar el PNG grande en novela visual; el sprite pequeño se usa sólo en batalla/exploración.
        if (heroArt == null) heroArt = HeroFrame("Idle_Front", 0) ?? angelFrames[0];
        if (demonIdleFrames[0] != null) demonArt = demonIdleFrames[0];
        musicVolume = PlayerPrefs.GetFloat("Inferno.MusicVolume", 0.65f);
        effectsVolume = PlayerPrefs.GetFloat("Inferno.EffectsVolume", 0.8f);
        fullscreen = PlayerPrefs.GetInt("Inferno.Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
        Screen.fullScreen = fullscreen;
        menuMusic = Resources.Load<AudioClip>("Audio/menu");
        heavenMusic = Resources.Load<AudioClip>("Audio/cielo");
        battleMusic = Resources.Load<AudioClip>("Audio/combate");
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        effectsSource = gameObject.AddComponent<AudioSource>();
        effectsSource.playOnAwake = false;
        effectsSource.spatialBlend = 0f;
        ApplyAudioVolumes();
        EnsureRuntimeCamera();
        SetupCanvas();
        phase = Phase.MainMenu;
        Draw();
    }

    void LoadNewHeroAnimations()
    {
        string[] names = {"Idle_Right", "Idle_Left", "Idle_Front", "Idle_Back",
                          "Walk_Right", "Walk_Left", "Walk_Front", "Walk_Back",
                          "Attack_Right", "Defeat_Right"};
        foreach (string name in names)
        {
            Sprite[] sprites = new Sprite[4];
            for (int i = 0; i < sprites.Length; i++)
                sprites[i] = Resources.Load<Sprite>("Art/HeroAnimations/" + name + "_" + i.ToString("00"));
            heroAnimations[name] = sprites;
        }
        attackBeam = Resources.Load<Sprite>("Art/HeroAnimations/Attack_Beam_Extra");
    }


    void LoadNewDemonAnimations()
    {
        for (int i = 0; i < 4; i++)
        {
            demonIdleFrames[i] = Resources.Load<Sprite>("Art/DemonV11/demon_idle_" + i.ToString("00"));
            demonAttackFrames[i] = Resources.Load<Sprite>("Art/DemonV11/demon_attack_" + i.ToString("00"));
            demonDeathFrames[i] = Resources.Load<Sprite>("Art/DemonV11/demon_death_" + i.ToString("00"));
        }
        if (demonIdleFrames[0] == null)
        {
            for (int i = 0; i < 4; i++)
            {
                demonIdleFrames[i] = Resources.Load<Sprite>("Art/demon_user_0_" + i);
                demonAttackFrames[i] = demonIdleFrames[i];
                demonDeathFrames[i] = demonIdleFrames[Mathf.Clamp(i, 0, 3)];
            }
        }
    }

    void LoadDialoguePortraits()
    {
        string[] keys = {
            "mc_basic", "mc_angry", "mc_sad", "mc_blush",
            "prince_basic", "prince_angry", "prince_sad", "prince_blush",
            "scholar_basic", "scholar_angry", "scholar_sad", "scholar_blush",
            "knight_basic", "knight_angry", "knight_sad", "knight_blush"
        };
        foreach (string key in keys)
        {
            Sprite sprite = Resources.Load<Sprite>("Art/DialogPortraits/" + key);
            if (sprite != null) dialoguePortraits[key] = sprite;
        }

        heroArt = GetDialoguePortrait("mc", "basic") ?? heroArt;
        princeArt = GetDialoguePortrait("prince", "basic") ?? princeArt;
        scholarArt = GetDialoguePortrait("scholar", "basic") ?? scholarArt;
    }

    string NormalizeExpression(string expression)
    {
        if (string.IsNullOrEmpty(expression)) return "basic";
        string e = expression.Trim().ToLowerInvariant();
        if (e.Contains("fur") || e.Contains("angry") || e.Contains("ofendid")) return "angry";
        if (e.Contains("trist") || e.Contains("sad") || e.Contains("nerv")) return "sad";
        if (e.Contains("blush") || e.Contains("rubor") || e.Contains("divertid")) return "blush";
        return "basic";
    }

    Sprite GetDialoguePortrait(string characterKey, string expression)
    {
        string normalized = NormalizeExpression(expression);
        Sprite portrait;
        if (dialoguePortraits.TryGetValue(characterKey + "_" + normalized, out portrait) && portrait != null)
            return portrait;
        if (dialoguePortraits.TryGetValue(characterKey + "_basic", out portrait) && portrait != null)
            return portrait;
        switch (characterKey)
        {
            case "mc": return heroArt;
            case "prince": return princeArt;
            case "scholar": return scholarArt;
            case "knight": return demonArt;
            default: return heroArt;
        }
    }

    Sprite GetPortraitForSpeaker(string speaker, string expression)
    {
        string key = "mc";
        if (!string.IsNullOrEmpty(speaker))
        {
            string s = speaker.Trim().ToUpperInvariant();
            if (s.Contains("PRÍNCIPE") || s.Contains("PRINCIPE")) key = "prince";
            else if (s.Contains("ERUDITO")) key = "scholar";
            else if (s.Contains("CABALLERO") || s.Contains("KNIGHT")) key = "knight";
            else if (s.Contains("PROTAGONISTA") || s.Contains("ENVIADA")) key = "mc";
        }
        return GetDialoguePortrait(key, expression);
    }

    Sprite GetSecondaryPortraitForSpeaker(string speaker)
    {
        if (string.IsNullOrEmpty(speaker)) return GetDialoguePortrait("prince", "basic");
        string s = speaker.Trim().ToUpperInvariant();
        if (s.Contains("ERUDITO")) return GetDialoguePortrait("prince", "basic");
        if (s.Contains("PRÍNCIPE") || s.Contains("PRINCIPE")) return GetDialoguePortrait("scholar", "basic");
        return GetDialoguePortrait("prince", "basic");
    }


    Sprite GetEndingPrimaryPortraitForSpeaker(string speaker, string expression)
    {
        if (!string.IsNullOrEmpty(speaker))
        {
            string s = speaker.Trim().ToUpperInvariant();
            if (s.Contains("SISTEMA")) return GetDialoguePortrait("prince", "basic");
        }
        return GetPortraitForSpeaker(speaker, expression);
    }

    Sprite HeroFrame(string sequence, int index)
    {
        Sprite[] frames;
        if (!heroAnimations.TryGetValue(sequence, out frames) || frames == null) return null;
        return frames[Mathf.Clamp(index, 0, frames.Length - 1)];
    }

    Sprite AnimatedHeroFrame(bool walking)
    {
        string sequence = (walking ? "Walk_" : "Idle_") + heroDirection;
        int frame = walking ? Mathf.FloorToInt(walkAnimation * 7f) % 4 : Mathf.FloorToInt(Time.time * 2f) % 4;
        return HeroFrame(sequence, frame) ?? heroArt;
    }


    Sprite AnimatedPrinceIdleFrame()
    {
        if (princeIdleFrames[0] == null) return princeArt;
        int available = princeIdleFrames[2] != null ? 4 : 2;
        int frame = Mathf.FloorToInt(Time.time * 2.6f) % available;
        return princeIdleFrames[frame] ?? princeIdleFrames[0] ?? princeArt;
    }

    Sprite AnimatedScholarIdleFrame()
    {
        if (scholarIdleFrames[0] == null) return scholarArt;
        int frame = Mathf.FloorToInt(Time.time * 2.2f) % scholarIdleFrames.Length;
        return scholarIdleFrames[frame] ?? scholarIdleFrames[0] ?? scholarArt;
    }

    Sprite AnimatedScrollFrame()
    {
        if (scrollIdleFrames[0] == null) return parchmentLight;
        int frame = Mathf.FloorToInt(Time.time * 2.0f) % scrollIdleFrames.Length;
        return scrollIdleFrames[frame] ?? scrollIdleFrames[0] ?? parchmentLight;
    }

    Sprite CurrentDemonFrame()
    {
        if (enemyHP <= 0)
        {
            int deathIndex = Mathf.Min(3, Mathf.FloorToInt((Time.time * 4f) % 4));
            return demonDeathFrames[deathIndex] ?? demonArt;
        }
        bool attacking = Time.time < enemyAttackUntil;
        int frame = Mathf.FloorToInt(Time.time * (attacking ? 8f : 3f)) % 4;
        return (attacking ? demonAttackFrames[frame] : demonIdleFrames[frame]) ?? demonArt;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (phase == Phase.Options) { phase = returnPhase; Draw(); }
            else if (phase != Phase.MainMenu) OpenMenu();
            return;
        }
        if (phase == Phase.ExploreHeaven || phase == Phase.ExplorePalace)
        {
            MoveCharacter();
            if (princeExploreImage != null && phase == Phase.ExplorePalace) princeExploreImage.sprite = AnimatedPrinceIdleFrame();
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space)) InteractExplorer();
            return;
        }
        if(phase==Phase.Battle)
        {
            var vsNode = stage.Find("Contra");
            if (vsNode != null) vsNode.GetComponent<Text>().text = Time.time < battleAttackUntil ? "¡IMPACTO!" : "VS";
            var enemyNode=stage.Find("Demonio sprite usuario");
            var heroNode=stage.Find("Heroína sprite usuario");
            if(enemyNode!=null)enemyNode.GetComponent<Image>().sprite=CurrentDemonFrame();
            if (heroNode != null)
            {
                bool attacking = Time.time < battleAttackUntil;
                int frame = attacking ? Mathf.Clamp(Mathf.FloorToInt((battleAttackUntil-Time.time)*-7f+4f),0,3) : Mathf.FloorToInt(Time.time*2f)%4;
                heroNode.GetComponent<Image>().sprite = (attacking ? HeroFrame("Attack_Right",frame) : HeroFrame("Idle_Right",frame)) ?? heroArt;
                var beam = stage.Find("Destello ataque");
                if (beam != null) beam.gameObject.SetActive(attacking);
            }
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (phase == Phase.Intro || phase == Phase.Palace || phase == Phase.ChoiceReply || phase == Phase.Ending)
                Advance();
        }
    }

    void SetupCanvas()
    {
        var c = new GameObject("Inferno UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = c.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = c.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        stage = c.GetComponent<RectTransform>();
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var e = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            e.transform.SetParent(transform);
        }
    }


    void EnsureRuntimeCamera()
    {
        if (FindFirstObjectByType<Camera>() != null) return;
        var camObj = new GameObject("Inferno Runtime Camera", typeof(Camera), typeof(AudioListener));
        var cam = camObj.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.03f, 0.04f, 0.07f, 1f);
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.nearClipPlane = -10f;
        cam.farClipPlane = 50f;
        cam.depth = -1f;
        camObj.tag = "MainCamera";
    }

    void OpenMenu() { phase = Phase.MainMenu; Draw(); }

    void BeginNewGame() { StartAgain(); }

    void ApplyAudioVolumes()
    {
        if (musicSource != null) musicSource.volume = musicVolume;
        if (effectsSource != null) effectsSource.volume = effectsVolume;
    }

    // Cada área tiene su tema. Las pantallas de opciones conservan el tema
    // de la escena desde la que se abrieron. No reiniciar el audio al redibujar
    // diálogos, indicadores o botones de combate.
    void SyncSceneMusic()
    {
        if (musicSource == null) return;
        Phase active = phase == Phase.Options ? returnPhase : phase;
        AudioClip desired;
        switch (active)
        {
            case Phase.Intro:
            case Phase.ExploreHeaven:
            case Phase.Parchment:
                desired = heavenMusic;
                break;
            case Phase.Battle:
                desired = battleMusic;
                break;
            default:
                // Menú, palacio, diálogos y pantallas de resultado.
                desired = menuMusic;
                break;
        }
        if (musicSource.clip == desired)
        {
            if (desired != null && !musicSource.isPlaying) musicSource.Play();
            return;
        }
        musicSource.Stop();
        musicSource.clip = desired;
        if (desired != null) musicSource.Play();
    }

    void SaveOptions()
    {
        PlayerPrefs.SetFloat("Inferno.MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("Inferno.EffectsVolume", effectsVolume);
        PlayerPrefs.SetInt("Inferno.Fullscreen", fullscreen ? 1 : 0);
        PlayerPrefs.Save();
        Screen.fullScreen = fullscreen;
        ApplyAudioVolumes();
        phase = returnPhase;
        Draw();
    }

    void OpenOptions()
    {
        returnPhase = phase;
        phase = Phase.Options;
        Draw();
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void StartAgain()
    {
        princeAffinity = 40; scholarAffinity = 40; choiceIndex = -1; step = 0;
        totalGold = 0; lastGoldLoot = 0; lastLootItem = "";
        phase = Phase.Intro; walkX=.16f; walkY=.42f; Draw();
    }

    void Clear()
    {
        for (int i = stage.childCount - 1; i >= 0; --i) Destroy(stage.GetChild(i).gameObject);
    }

    RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        var g = new GameObject(name, typeof(RectTransform));
        g.transform.SetParent(parent, false);
        var rt = g.GetComponent<RectTransform>(); rt.anchorMin = min; rt.anchorMax = max;
        rt.offsetMin = offsetMin; rt.offsetMax = offsetMax; return rt;
    }
    RectTransform Panel(string name, Transform parent, Vector2 min, Vector2 max, Color color, Vector2? low = null, Vector2? high = null)
    {
        var rt = Rect(name, parent, min, max, low ?? Vector2.zero, high ?? Vector2.zero);
        var image = rt.gameObject.AddComponent<Image>(); image.color = color; return rt;
    }
    Button ClickArea(string name, Transform parent, Vector2 min, Vector2 max, Action action)
    {
        var rt = Rect(name, parent, min, max, Vector2.zero, Vector2.zero);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = new Color(1f,1f,1f,0.001f);
        var btn = rt.gameObject.AddComponent<Button>();
        btn.onClick.AddListener(() => action());
        return btn;
    }
    Text Label(string name, Transform parent, string value, int size, Color color, Vector2 min, Vector2 max,
        TextAnchor align = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal, Vector2? low = null, Vector2? high = null)
    {
        var rt = Rect(name, parent, min, max, low ?? Vector2.zero, high ?? Vector2.zero);
        var t = rt.gameObject.AddComponent<Text>(); t.text = value;
        bool displayType = name.Contains("Título") || name.Contains("Titulo") || name.Contains("Logo") || name.Contains("Seccion") || name.Contains("GameTitle");
        t.font = displayType ? headingFont : font; t.fontSize = size;
        t.color = color; t.alignment = align; t.fontStyle = style;
        var shadow = rt.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.015f, 0.022f, 0.047f, 0.94f);
        shadow.effectDistance = new Vector2(1.35f, -1.35f);
        shadow.useGraphicAlpha = true;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
        t.supportRichText = true; return t;
    }
    void SoftShadow(Text t, Color? color = null, Vector2? distance = null)
    {
        if (t == null) return;
        var shadow = t.GetComponent<Shadow>();
        if (shadow == null) shadow = t.gameObject.AddComponent<Shadow>();
        shadow.effectColor = color ?? new Color(0.02f, 0.02f, 0.04f, 0.72f);
        shadow.effectDistance = distance ?? new Vector2(1.1f, -1.1f);
        shadow.useGraphicAlpha = true;
    }
    void Picture(string name, Transform parent, Sprite sprite, Vector2 min, Vector2 max, Color tint)
    {
        var rt = Panel(name, parent, min, max, tint);
        var img = rt.GetComponent<Image>(); img.sprite = sprite; img.preserveAspect = name.Contains("sprite usuario") || name.Contains("Retrato");
        img.raycastTarget = false;
    }
    Button Btn(string text, Transform parent, Vector2 min, Vector2 max, Action action, Color? col = null, int textSize = 18)
    {
        var rt = ParchmentPanel("Boton " + text, parent, min, max, col ?? ffBlue2);
        var button = rt.gameObject.AddComponent<Button>();
        if (v10Menu != null) { Skin(rt, text.Contains("SKILL") || text.Contains("LANZA") ? v10MenuRed : v10Menu); rt.GetComponent<Image>().raycastTarget=true; }
        var colors = button.colors; colors.highlightedColor = new Color32(90, 121, 147, 255);
        colors.pressedColor = new Color32(148, 99, 73, 255); button.colors = colors;
        button.onClick.AddListener(() => action());
        var buttonText=Label("Texto boton", rt, text, textSize, ivory,
            new Vector2(.065f,.16f), new Vector2(.935f,.84f), TextAnchor.MiddleCenter, FontStyle.Bold);
        if (text.StartsWith("A  /  ") || text.StartsWith("B  /  ") || text.StartsWith("C  /  "))
        {
            buttonText.resizeTextForBestFit=true;
            buttonText.resizeTextMinSize=17;
            buttonText.resizeTextMaxSize=21;
            SoftShadow(buttonText,new Color(0f,0f,0f,.94f),new Vector2(1.4f,-1.4f));
        }
        return button;
    }
    void Header(string section)
    {
        // La ilustración de la cabecera tiene adornos grandes en sus extremos
        // y un emblema central: reservar zonas interiores independientes.
        ParchmentPanel("Cabecera", stage, new Vector2(0f,.905f), Vector2.one,
            new Color32(12,23,45,244));

        // Evita que la tipografía se dibuje encima del ornamento izquierdo.
        var titleZone = Rect("Area interior titulo", stage,
            new Vector2(.18f,.912f), new Vector2(.455f,.955f),
            Vector2.zero, Vector2.zero);
        titleZone.gameObject.AddComponent<RectMask2D>();
        var title = Label("Logo", titleZone, "INFERNO / LA TREGUA IMPOSIBLE",
            18, gold, new Vector2(.015f,0f), new Vector2(.985f,1f),
            TextAnchor.MiddleCenter, FontStyle.Bold);
        title.resizeTextForBestFit = true;
        title.resizeTextMinSize = 11;
        title.resizeTextMaxSize = 18;
        title.horizontalOverflow = HorizontalWrapMode.Wrap;
        title.verticalOverflow = VerticalWrapMode.Truncate;

        // Reservar otra zona para el capítulo, sin pisar la espada ni el menú.
        var chapterZone = Rect("Area interior capitulo", stage,
            new Vector2(.705f,.912f), new Vector2(.862f,.955f),
            Vector2.zero, Vector2.zero);
        chapterZone.gameObject.AddComponent<RectMask2D>();
        var chapter = Label("Seccion", chapterZone, section,
            14, ivory, new Vector2(.015f,0f), new Vector2(.985f,1f),
            TextAnchor.MiddleCenter);
        chapter.resizeTextForBestFit = true;
        chapter.resizeTextMinSize = 10;
        chapter.resizeTextMaxSize = 14;
        chapter.horizontalOverflow = HorizontalWrapMode.Wrap;
        chapter.verticalOverflow = VerticalWrapMode.Truncate;

        Btn("MENÚ",stage,new Vector2(.877f,.914f),new Vector2(.965f,.960f),
            OpenMenu,ffBlue2,12);
    }
    void Background(bool heaven = false)
    {
        Picture("Fondo", stage, heaven ? bgHeaven : bgPalace, Vector2.zero, Vector2.one, Color.white);
        Panel("Oscurecer", stage, Vector2.zero, Vector2.one, heaven ? new Color(0.03f,.04f,.08f,.18f) : new Color(.03f,.02f,.07f,.24f));
    }
    void BackgroundPrinceDialogue()
    {
        Picture("Fondo dialogo príncipe", stage, bgPalaceDialogue != null ? bgPalaceDialogue : bgPalace, Vector2.zero, Vector2.one, Color.white);
        Panel("Oscurecer dialogo", stage, Vector2.zero, Vector2.one, new Color(.03f,.02f,.07f,.18f));
    }

    void BackgroundVictory()
    {
        Picture("Fondo victoria", stage, bgVictory != null ? bgVictory : bgPalaceDialogue != null ? bgPalaceDialogue : bgPalace, Vector2.zero, Vector2.one, Color.white);
        Panel("Oscurecer victoria", stage, Vector2.zero, Vector2.one, new Color(.02f,.02f,.06f,.30f));
    }
    void Portrait(Sprite sprite, Vector2 min, Vector2 max, Color? tint = null)
    {
        Picture("Retrato", stage, sprite, min, max, tint ?? Color.white);
    }
    void FrameLines(Transform parent, Color border) { } // V10 no superpone filetes decorativos

    RectTransform ParchmentPanel(string title, Transform parent, Vector2 min, Vector2 max, Color? color = null)
    {
        var r=Panel(title, parent, min, max, color ?? ffBlue);
        if (title.Contains("Caja de dialogo")) Skin(r, v10Dialogue);
        else if (title.Contains("Boton")) { } // handled by Btn
        else if (title.Contains("Cabecera")) Skin(r, v10Header);
        else if (title.Contains("Registro") || title.Contains("Comandos") || title.Contains("Overlay_Central") || title.Contains("Panel opciones")) Skin(r, v10Panel);
        return r;
    }

    void Skin(RectTransform panel, Sprite sprite, Color? tint = null)
    {
        if (panel == null || sprite == null) return;
        Image image=panel.GetComponent<Image>();
        if (image == null) image=panel.gameObject.AddComponent<Image>();
        image.sprite=sprite;
        image.type=Image.Type.Simple;
        image.color=tint ?? Color.white;
        image.raycastTarget=false;
    }

    void PortraitMedallion(RectTransform stats, Sprite portrait, bool left)
    {
        if (portrait == null) return;
        Vector2 min = left ? new Vector2(.035f,.12f) : new Vector2(.80f,.18f);
        Vector2 max = left ? new Vector2(.23f,.91f) : new Vector2(.94f,.86f);
        var frame=Rect("Retrato circular HUD", stats, min, max, Vector2.zero,Vector2.zero);
        var image=frame.gameObject.AddComponent<Image>();
        image.sprite=portrait;
        image.preserveAspect=true;
        image.raycastTarget=false;
    }

    void ParchmentBackdrop()
    {
        Panel("Velo pergamino", stage, new Vector2(.16f,.08f), new Vector2(.84f,.90f), new Color(0f,0f,0f,.10f));
    }

    void Affinity(bool detailed = false)
    {
        // Fondo carbón neutro para no competir con los tonos rojos del palacio.
        // Reservar más altura para que las afinidades se lean en modo Game reducido.
        var bar=Panel("Panel de vínculos",stage,new Vector2(.275f,.755f),new Vector2(.725f,.895f),new Color(.065f,.055f,.07f,.91f));
        Panel("Acento superior afinidad",bar,new Vector2(.02f,.965f),new Vector2(.98f,.98f),gold);
        var princeLabel=Label("Afinidad príncipe",bar,"PRÍNCIPE  ♥  "+princeAffinity+"%",20,ivory,
            new Vector2(.04f,.55f),new Vector2(.52f,.94f),TextAnchor.MiddleLeft,FontStyle.Bold);
        Meter(bar,.54f,.66f,.41f,.14f,princeAffinity/100f,new Color32(245,151,192,255));
        var scholarLabel=Label("Afinidad erudito",bar,"ERUDITO  ♥  "+scholarAffinity+"%",20,ivory,
            new Vector2(.04f,.09f),new Vector2(.52f,.47f),TextAnchor.MiddleLeft,FontStyle.Bold);
        Meter(bar,.54f,.19f,.41f,.14f,scholarAffinity/100f,new Color32(192,152,240,255));
        SoftShadow(princeLabel,new Color(0f,0f,0f,.85f),new Vector2(1.25f,-1.25f));
        SoftShadow(scholarLabel,new Color(0f,0f,0f,.85f),new Vector2(1.25f,-1.25f));
        if (detailed)
        {
            var tip=Label("Pista afinidades",stage,"Tus respuestas modifican las relaciones y pueden abrir nuevas rutas.",18,ivory,
                new Vector2(.26f,.69f),new Vector2(.74f,.75f),TextAnchor.MiddleCenter,FontStyle.Bold);
            SoftShadow(tip,new Color(0f,0f,0f,.98f),new Vector2(1.7f,-1.7f));
        }
    }
    void Meter(Transform parent, float x, float y, float width, float height, float value, Color color)
    {
        Panel("Medidor base", parent, new Vector2(x,y), new Vector2(x+width,y+height), new Color32(37, 38, 59, 255));
        Panel("Medidor valor", parent, new Vector2(x,y), new Vector2(x+width*Mathf.Clamp01(value),y+height), color);
    }
    void BattleBar(Transform parent, string name, string caption, float value, Color fill, Vector2 min, Vector2 max)
    {
        var holder = Rect(name, parent, min, max, Vector2.zero, Vector2.zero);
        Label(name + " cap", holder, caption, 12, ivory, new Vector2(0f,.57f), new Vector2(.30f,1f), TextAnchor.MiddleLeft, FontStyle.Bold);
        Panel(name + " base", holder, new Vector2(.32f,.18f), new Vector2(.98f,.72f), new Color32(28,33,47,245));
        Panel(name + " fill", holder, new Vector2(.32f,.18f), new Vector2(.32f + .66f*Mathf.Clamp01(value), .72f), fill);
        Panel(name + " shine", holder, new Vector2(.32f,.48f), new Vector2(.98f,.72f), new Color(1f,1f,1f,.06f));
    }
    void DialogueUI(Line line, string next = "CONTINUAR  >")
    {
        var box = ParchmentPanel("Caja de dialogo", stage, new Vector2(.06f,.045f), new Vector2(.94f,.305f), new Color32(12,23,45,244));
        Label("Hablante", box, line.speaker + (line.expression == "normal" ? "" : "  /  " + line.expression.ToUpper()), 21, gold,
            new Vector2(.04f,.75f), new Vector2(.68f,.96f), style:FontStyle.Bold);
        Label("Dialogo", box, line.message, 26, ivory, new Vector2(.04f,.21f), new Vector2(.96f,.73f));
        Btn(next, box, new Vector2(.72f,.045f), new Vector2(.965f,.215f), Advance, ffBlue2, 16);
        Label("Tecla", stage, "CLICK / ESPACIO PARA AVANZAR", 13, ivory, new Vector2(.61f,.008f), new Vector2(.95f,.041f), TextAnchor.MiddleRight);
    }
    void Draw()
    {
        SyncSceneMusic();
        Clear();
        switch(phase)
        {
            case Phase.MainMenu: DrawMainMenu(); break;
            case Phase.Options: DrawOptions(); break;
            case Phase.Intro:
                var introLine = intro[Mathf.Clamp(step,0,intro.Count-1)];
                Background(true); Portrait(GetPortraitForSpeaker(introLine.speaker, introLine.expression), new Vector2(.58f,.10f), new Vector2(.91f,.79f)); Header("01 / EL DESCENSO");
                DialogueUI(introLine); break;
            case Phase.ExploreHeaven:
                DrawExplore(true); break;
            case Phase.ExplorePalace:
                DrawExplore(false); break;
            case Phase.Parchment:
                Background(true); Header("01 / EL PERGAMINO");
                var paper = Panel("Pergamino de la Luz", stage, new Vector2(.31f,.16f), new Vector2(.69f,.84f), new Color32(244,232,198,248));
                Picture("Retrato pergamino", paper, AnimatedScrollFrame(), new Vector2(.36f,.79f), new Vector2(.64f,.96f), Color.white);
                Label("Sello", paper, "✦ CONSEJO SUPERIOR ✦", 28, ink, new Vector2(.08f,.67f), new Vector2(.92f,.79f), TextAnchor.MiddleCenter, FontStyle.Bold);
                Label("Orden", paper, "ORDEN OFICIAL\n\nEstablecer contacto con el reino de Inferno.\n\nSe prohíbe iniciar hostilidades sin autorización del Consejo.", 22,
                    ink, new Vector2(.12f,.31f), new Vector2(.88f,.66f), TextAnchor.MiddleCenter);
                Btn("HE LEÍDO LA ORDEN", paper, new Vector2(.21f,.08f), new Vector2(.79f,.19f), () => {phase=Phase.ExplorePalace;walkX=.16f;walkY=.42f;step=0;Draw();}, ffBlue2, 17);
                break;
            case Phase.Palace:
                var palaceLine = palace[Mathf.Clamp(step,0,palace.Count-1)];
                BackgroundPrinceDialogue();
                if (palaceLine.speaker == "PROTAGONISTA")
                {
                    Portrait(GetPortraitForSpeaker(palaceLine.speaker, palaceLine.expression),new Vector2(.05f,.22f),new Vector2(.34f,.87f));
                    Portrait(GetDialoguePortrait("prince", "basic"),new Vector2(.59f,.24f),new Vector2(.84f,.87f));
                    Portrait(GetDialoguePortrait("scholar", "basic"),new Vector2(.80f,.29f),new Vector2(.97f,.80f),new Color(1,1,1,.94f));
                }
                else
                {
                    Portrait(GetPortraitForSpeaker(palaceLine.speaker, palaceLine.expression),new Vector2(.56f,.23f),new Vector2(.85f,.87f));
                    Portrait(GetSecondaryPortraitForSpeaker(palaceLine.speaker),new Vector2(.81f,.29f),new Vector2(.98f,.80f),new Color(1,1,1,.94f));
                }
                Header("02 / PALACIO DE INFERNO"); Affinity(); DialogueUI(palaceLine); break;
            case Phase.Choice:
                BackgroundPrinceDialogue(); Portrait(GetDialoguePortrait("prince", "basic"),new Vector2(.62f,.26f),new Vector2(.96f,.86f));
                Header("02 / TU DECISIÓN"); Affinity(true);
                // Sin rectángulo azul detrás: solo título y botones ornamentales.
                var question=Label("Pregunta",stage,"¿QUÉ LE RESPONDES AL PRÍNCIPE?",27,gold,
                    new Vector2(.075f,.57f),new Vector2(.68f,.65f),TextAnchor.MiddleCenter,FontStyle.Bold);
                SoftShadow(question,new Color(0f,0f,0f,1f),new Vector2(2f,-2f));
                Btn("A  /  Vine a escuchar y evitar otra guerra.",stage,new Vector2(.07f,.415f),new Vector2(.67f,.525f),()=>Choose(0),textSize:21);
                Btn("B  /  ¿Empezamos o seguimos perdiendo el tiempo?",stage,new Vector2(.07f,.28f),new Vector2(.67f,.39f),()=>Choose(1),textSize:21);
                Btn("C  /  Guardar silencio y revisar los documentos.",stage,new Vector2(.07f,.145f),new Vector2(.67f,.255f),()=>Choose(2),textSize:21);
                break;
            case Phase.ChoiceReply:
                var replyLine = choiceIndex==2 ? new Line("PRÍNCIPE","Eres una simple sirvienta con un ego más grande del que mereces. ¡Veamos de qué eres capaz!", "ofendido") :
                    new Line("PRÍNCIPE","Al menos tienes agallas, sirvienta de la luz. Veamos qué puedes hacer...", "divertido");
                BackgroundPrinceDialogue(); Portrait(GetPortraitForSpeaker(replyLine.speaker, replyLine.expression),new Vector2(.57f,.25f),new Vector2(.9f,.85f));Header("02 / CONSECUENCIA");Affinity();
                DialogueUI(replyLine,"INICIAR LA PRUEBA  >");
                Label("Cambio",stage,choiceIndex==0?"AFINIDAD PRÍNCIPE  +5  ♥":choiceIndex==1?"AFINIDAD PRÍNCIPE  +10  ♥":"AFINIDAD PRÍNCIPE  -5",21,
                    choiceIndex==2?red:gold,new Vector2(.07f,.67f),new Vector2(.58f,.75f),style:FontStyle.Bold);
                break;
            case Phase.Battle: DrawBattle(); break;
            case Phase.Loot: DrawLoot(); break;
            case Phase.Ending:
                var endingLine = ending[Mathf.Clamp(step,0,ending.Count-1)];
                BackgroundPrinceDialogue();
                Portrait(GetDialoguePortrait("mc", "basic"),new Vector2(.01f,.3f),new Vector2(.36f,.89f));
                Portrait(GetEndingPrimaryPortraitForSpeaker(endingLine.speaker, endingLine.expression),new Vector2(.64f,.26f),new Vector2(.92f,.87f));
                if (!string.IsNullOrEmpty(endingLine.speaker) && endingLine.speaker.ToUpperInvariant().Contains("SISTEMA"))
                    Portrait(GetDialoguePortrait("scholar", "basic"),new Vector2(.80f,.34f),new Vector2(.98f,.79f),new Color(1,1,1,.94f));
                Header("03 / FINAL DEL COMBATE");Affinity();
                DialogueUI(endingLine,step==ending.Count-1?"VER RESULTADO  >":"CONTINUAR  >");break;
            case Phase.Summary: DrawSummary(); break;
            case Phase.Defeat:
                Background(); Header("DERROTA");
                Picture("Caída heroína",stage,HeroFrame("Defeat_Right",3) ?? heroArt,
                    new Vector2(.06f,.20f),new Vector2(.32f,.63f),Color.white);
                Label("Derrota",stage,"LA MISIÓN HA FRACASADO",44,red,new Vector2(.18f,.48f),new Vector2(.82f,.67f),TextAnchor.MiddleCenter,FontStyle.Bold);
                Label("Consejo",stage,"El enemigo agotó tus puntos de vida. Puedes reintentar esta prueba sin reiniciar la historia.",22,ivory,new Vector2(.19f,.32f),new Vector2(.81f,.45f),TextAnchor.MiddleCenter);
                Btn("REINTENTAR COMBATE",stage,new Vector2(.32f,.17f),new Vector2(.68f,.27f),StartBattle);break;
        }
    }
    Button MenuButton(string caption, Transform parent, Vector2 bottomLeft, Vector2 topRight, Action action)
    {
        return Btn(caption, parent, bottomLeft, topRight, action, ffBlue2, 24);
    }

    void MenuBackdrop()
    {
        Picture("Background_PixelArt",stage,menuBackground,Vector2.zero,Vector2.one,Color.white);
        ParchmentPanel("Overlay_Central",stage,new Vector2(.28f,.14f),new Vector2(.72f,.89f),new Color32(12,23,45,244));
    }

    void DrawMainMenu()
    {
        MenuBackdrop();
        var heading=Rect("TitlePanel",stage,new Vector2(.23f,.61f),new Vector2(.77f,.89f),Vector2.zero,Vector2.zero);
        Panel("OrnamentoSuperior",heading,new Vector2(.35f,.92f),new Vector2(.65f,.933f),gold);
        var title=Label("GameTitle",heading,"I N F E R N O",83,gold,new Vector2(0,.30f),new Vector2(1,.88f),TextAnchor.MiddleCenter,FontStyle.Bold);
        title.resizeTextForBestFit=true; title.resizeTextMinSize=40; title.resizeTextMaxSize=83;
        // Sombra sutil general se aplica en Label().
        Label("Subtitle",heading,"L A   T R E G U A   I M P O S I B L E",20,ivory,new Vector2(.05f,.12f),new Vector2(.95f,.32f),TextAnchor.MiddleCenter);
        Panel("OrnamentoInferior",heading,new Vector2(.31f,.08f),new Vector2(.69f,.09f),gold);
        var menu=Rect("MenuPanel",stage,new Vector2(.335f,.18f),new Vector2(.665f,.59f),Vector2.zero,Vector2.zero);
        MenuButton("NUEVA PARTIDA",menu,new Vector2(0,.70f),new Vector2(1,.93f),BeginNewGame);
        MenuButton("OPCIONES",menu,new Vector2(0,.39f),new Vector2(1,.62f),OpenOptions);
        MenuButton("SALIR",menu,new Vector2(0,.08f),new Vector2(1,.31f),QuitGame);
        Label("FooterPanel",stage,"DEMO V0.7  •  UNITY 6",16,ivory,new Vector2(.02f,.02f),new Vector2(.24f,.065f));
    }

    Slider MakeSlider(Transform parent, Vector2 min, Vector2 max, float startValue, Action<float> changed)
    {
        var area = ParchmentPanel("Barra ajuste",parent,min,max,new Color32(12,23,45,244));
        var fillArea=Rect("Fill Area",area,new Vector2(.04f,.27f),new Vector2(.96f,.73f),Vector2.zero,Vector2.zero);
        var fill=Panel("Fill",fillArea,Vector2.zero,Vector2.one,gold);
        var handleArea=Rect("Handle Slide Area",area,new Vector2(.04f,0),new Vector2(.96f,1),Vector2.zero,Vector2.zero);
        var handle=Panel("Handle",handleArea,new Vector2(0,.05f),new Vector2(0,.95f),new Color32(214, 220, 218, 255));
        handle.sizeDelta=new Vector2(17,0);
        var slider=area.gameObject.AddComponent<Slider>();
        slider.fillRect=fill;
        slider.handleRect=handle;
        slider.targetGraphic=handle.GetComponent<Image>();
        slider.direction=Slider.Direction.LeftToRight;
        slider.minValue=0f;slider.maxValue=1f;
        slider.wholeNumbers=false;
        slider.value=Mathf.Clamp01(startValue);
        slider.onValueChanged.AddListener(v => changed(v));
        return slider;
    }

    void DrawOptions()
    {
        MenuBackdrop();
        Panel("Sombra ajustes",stage,Vector2.zero,Vector2.one,new Color(.025f,.012f,.04f,.24f));
        var card=ParchmentPanel("Panel opciones",stage,new Vector2(.22f,.12f),new Vector2(.78f,.88f),new Color32(12,23,45,244));
        Label("Título opciones",card,"OPCIONES",37,gold,new Vector2(.07f,.83f),new Vector2(.93f,.96f),TextAnchor.MiddleCenter,FontStyle.Bold);
        Label("Música",card,"VOLUMEN DE MÚSICA",18,ivory,new Vector2(.09f,.68f),new Vector2(.91f,.76f),style:FontStyle.Bold);
        var musicValue=Label("Porcentaje música",card,Mathf.RoundToInt(musicVolume*100)+"%",16,gold,new Vector2(.79f,.61f),new Vector2(.91f,.68f),TextAnchor.MiddleRight);
        MakeSlider(card,new Vector2(.09f,.60f),new Vector2(.77f,.68f),musicVolume,v => {musicVolume=v;musicValue.text=Mathf.RoundToInt(v*100)+"%";ApplyAudioVolumes();});
        Label("Efectos",card,"VOLUMEN DE EFECTOS",18,ivory,new Vector2(.09f,.47f),new Vector2(.91f,.55f),style:FontStyle.Bold);
        var effectsValue=Label("Porcentaje efectos",card,Mathf.RoundToInt(effectsVolume*100)+"%",16,gold,new Vector2(.79f,.40f),new Vector2(.91f,.47f),TextAnchor.MiddleRight);
        MakeSlider(card,new Vector2(.09f,.39f),new Vector2(.77f,.47f),effectsVolume,v => {effectsVolume=v;effectsValue.text=Mathf.RoundToInt(v*100)+"%";ApplyAudioVolumes();});
        var fullscreenButton=Btn(fullscreen?"PANTALLA COMPLETA: SÍ":"PANTALLA COMPLETA: NO",card,new Vector2(.10f,.235f),new Vector2(.90f,.33f),()=>{
            fullscreen=!fullscreen;
            Draw();
        },ffBlue2,18);
        Btn("GUARDAR Y VOLVER",card,new Vector2(.1f,.09f),new Vector2(.9f,.18f),SaveOptions,ffBlue2,19);
        Label("Nota audio",stage,"Los controles de audio quedan preparados para la música y los efectos que agreguemos después.",13,ivory,new Vector2(.19f,.045f),new Vector2(.81f,.10f),TextAnchor.MiddleCenter);
    }

    void DrawExplore(bool heaven)
    {
        Picture("Escenario explorable",stage,heaven?skyWalk:palaceWalk,Vector2.zero,Vector2.one,Color.white);
        Header(heaven?"01 / ATRIO CELESTIAL":"02 / SALÓN DEL TRONO");
        var objective = ParchmentPanel("Objetivo de exploración",stage,new Vector2(.045f,.765f),new Vector2(.44f,.89f));
        Panel("Borde objetivo",objective,new Vector2(0,.95f),Vector2.one,gold);
        Label("Título objetivo",objective,heaven?"MISIÓN  /  EXAMINA EL PERGAMINO":"MISIÓN  /  HABLA CON EL PRÍNCIPE",20,gold,new Vector2(.05f,.48f),new Vector2(.98f,.9f),style:FontStyle.Bold);
        Label("Texto objetivo",objective,"Camina hasta el marcador dorado y pulsa E o haz clic.",17,ivory,new Vector2(.05f,.06f),new Vector2(.98f,.46f));
        if (heaven)
        {
            var scrollSpot = Rect("Pergamino objetivo",stage,new Vector2(.77f,.37f),new Vector2(.90f,.57f),Vector2.zero,Vector2.zero);
            var scrollImg = scrollSpot.gameObject.AddComponent<Image>();
            scrollImg.sprite = AnimatedScrollFrame();
            scrollImg.preserveAspect = true;
            scrollImg.raycastTarget = false;
            ClickArea("Click pergamino", stage, new Vector2(.76f,.35f), new Vector2(.91f,.58f), ()=>InteractExplorer(true));
        }
        else
        {
            var scholarSpot = Rect("Erudito objetivo",stage,new Vector2(.78f,.33f),new Vector2(.86f,.55f),Vector2.zero,Vector2.zero);
            var scholarImg = scholarSpot.gameObject.AddComponent<Image>();
            scholarImg.sprite = AnimatedScholarIdleFrame();
            scholarImg.preserveAspect = true;
            scholarImg.raycastTarget = false;

            var princeSpot = Rect("Príncipe objetivo",stage,new Vector2(.72f,.34f),new Vector2(.82f,.62f),Vector2.zero,Vector2.zero);
            princeExploreImage = princeSpot.gameObject.AddComponent<Image>();
            princeExploreImage.sprite = AnimatedPrinceIdleFrame();
            princeExploreImage.preserveAspect = true;
            princeExploreImage.raycastTarget = false;
            ClickArea("Click príncipe", stage, new Vector2(.71f,.32f), new Vector2(.86f,.62f), ()=>InteractExplorer(true));
        }
        walker=Rect("Protagonista caminando",stage,new Vector2(walkX-.055f,walkY-.005f),new Vector2(walkX+.055f,walkY+.255f),Vector2.zero,Vector2.zero);
        walkerImage=walker.gameObject.AddComponent<Image>();walkerImage.sprite=AnimatedHeroFrame(false);
        walkerImage.preserveAspect=true;walkerImage.raycastTarget=false;
        var hint = ParchmentPanel("Guía",stage,new Vector2(.29f,.025f),new Vector2(.71f,.103f));
        Panel("Guía borde",hint,new Vector2(0,.96f),Vector2.one,gold);
        movementHint=Label("Controles",hint,"WASD / FLECHAS: MOVER   ·   E: INTERACTUAR",17,ivory,new Vector2(.03f,.1f),new Vector2(.97f,.9f),TextAnchor.MiddleCenter,FontStyle.Bold);
        Btn("IR AL OBJETIVO",stage,new Vector2(.77f,.025f),new Vector2(.97f,.115f),()=>{ walkX=.8f;walkY=.43f;PlaceWalker();InteractExplorer(); },ffBlue2,15);
    }
    void MoveCharacter()
    {
        float dx=0,dy=0;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) dx-=1;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) dx+=1;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) dy+=1;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) dy-=1;
        Vector2 dir = new Vector2(dx,dy).normalized;
        moving = dir.sqrMagnitude > 0;
        float speed = (Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift)) ? .37f : .22f;
        // Colision por zonas navegables en coordenadas Canvas (equivalente a limites 2D para UI).
        Vector2 nextX = new Vector2(walkX+dir.x*speed*Time.deltaTime,walkY);
        if (CanWalk(nextX, phase == Phase.ExploreHeaven)) walkX = nextX.x;
        float verticalFactor = dir.y > 0.1f ? 0.72f : 0.54f;
        Vector2 nextY = new Vector2(walkX,walkY+dir.y*speed*verticalFactor*Time.deltaTime);
        if (CanWalk(nextY, phase == Phase.ExploreHeaven)) walkY = nextY.y;
        PlaceWalker();
        if (dir.x > 0.1f) heroDirection = "Right";
        else if (dir.x < -0.1f) heroDirection = "Left";
        else if (dir.y > 0.1f) heroDirection = "Back";
        else if (dir.y < -0.1f) heroDirection = "Front";
        if (moving) walkAnimation += Time.deltaTime;
        if (walkerImage != null) walkerImage.sprite = AnimatedHeroFrame(moving);
        if (movementHint != null) movementHint.text=walkX>.66f ? "ESTÁS CERCA · PULSA E PARA INTERACTUAR" : "WASD / FLECHAS: MOVER  ·  SHIFT: CORRER  ·  E: INTERACTUAR";
    }
    // El personaje usa RectTransform en Screen Space. Por eso las colisiones se resuelven
    // en el espacio normalizado del mapa, en vez de usar Physics2D en coordenadas de mundo.
    // Fronteras de los corredores cenitales: cada muestra fija el borde inferior y superior.
    // Interpolando entre segmentos se pueden recorrer las curvas y estrechamientos sin
    // permitir caminar por el vacío, las columnas o la plataforma del trono.
    bool CanWalk(Vector2 position, bool heaven)
    {
        if (heaven)
        {
            if (position.x < .095f || position.x > .85f) return false;
            float[] xs={.095f,.16f,.23f,.30f,.38f,.48f,.60f,.68f,.76f,.85f};
            float[] bottoms={.31f,.31f,.31f,.31f,.31f,.31f,.29f,.22f,.18f,.18f};
            float[] tops={.60f,.60f,.60f,.60f,.60f,.61f,.64f,.73f,.69f,.66f};
            int k=0;
            while(k<xs.Length-2 && position.x>xs[k+1]) k++;
            float t=Mathf.InverseLerp(xs[k],xs[k+1],position.x);
            t=t*t*(3f-2f*t);
            float lower=Mathf.Lerp(bottoms[k],bottoms[k+1],t)+.012f;
            float upper=Mathf.Lerp(tops[k],tops[k+1],t)-.012f;
            return position.y>=lower && position.y<=upper;
        }

        bool InRect(float xMin,float yMin,float xMax,float yMax) =>
            position.x >= xMin && position.x <= xMax && position.y >= yMin && position.y <= yMax;
        bool InEllipse(float cx,float cy,float rx,float ry)
        {
            float dx=(position.x-cx)/Mathf.Max(.0001f,rx);
            float dy=(position.y-cy)/Mathf.Max(.0001f,ry);
            return dx*dx + dy*dy <= 1f;
        }

        // Zonas transitables del nuevo cuarto infernal cenital.
        // Se modelan como unión de salas/corredores amplios con exclusiones
        // para columnas y mobiliario, evitando que la protagonista atraviese
        // muros o quede bloqueada antes de llegar al príncipe.
        bool walkable =
            InRect(.11f,.18f,.38f,.76f) ||      // salón principal izquierdo
            InRect(.33f,.22f,.67f,.63f) ||      // paso central hacia la derecha
            InRect(.63f,.28f,.77f,.60f) ||      // antesala del trono / mesa lateral
            InEllipse(.88f,.45f,.14f,.23f) ||   // cámara circular del trono
            InRect(.74f,.33f,.96f,.57f);        // apoyo recto para acercarse al objetivo

        if (!walkable) return false;

        bool blocked =
            InRect(.00f,.00f,.11f,1f)  || InRect(.97f,.00f,1f,1f) ||   // muros laterales
            InRect(.18f,.72f,.35f,.92f) ||                              // mesa superior
            InEllipse(.70f,.47f,.052f,.115f) ||                         // mesa ovalada derecha
            InRect(.91f,.31f,.99f,.58f) ||                              // trono
            InRect(.10f,.00f,.165f,.18f) ||                             // columna inferior izq
            InRect(.43f,.00f,.49f,.19f)  ||                             // columna inferior centro
            InRect(.68f,.00f,.76f,.15f)  ||                             // candelabro / soporte inferior der
            InRect(.105f,.57f,.165f,.97f) ||                            // columna superior izq
            InRect(.43f,.63f,.50f,.98f)  ||                             // columna superior centro
            InRect(.71f,.60f,.79f,.98f)  ||                             // columna superior der
            InRect(.20f,.10f,.29f,.18f)  || InRect(.34f,.10f,.43f,.18f) ||
            InRect(.76f,.14f,.85f,.23f);                                // candelabros

        return !blocked;
    }

    void PlaceWalker()
    {
        if (walker==null)return;
        walker.anchorMin=new Vector2(walkX-.055f,walkY-.005f);
        walker.anchorMax=new Vector2(walkX+.055f,walkY+.255f);
    }
    void InteractExplorer(bool clicked=false)
    {
        if (phase!=Phase.ExploreHeaven && phase!=Phase.ExplorePalace)return;
        if (!clicked && walkX < .66f) {if(movementHint!=null)movementHint.text="ACÉRCATE AL MARCADOR DORADO PARA INTERACTUAR"; return;}
        bool heaven=phase==Phase.ExploreHeaven;
        walker=null;walkerImage=null;movementHint=null;princeExploreImage=null;
        phase=heaven?Phase.Parchment:Phase.Palace;step=0;Draw();
    }

    void Advance()
    {
        if(phase==Phase.Intro) {step++;if(step>=intro.Count){phase=Phase.ExploreHeaven;walkX=.16f;walkY=.42f;step=0;}Draw();}
        else if(phase==Phase.Palace){step++;if(step>=palace.Count){phase=Phase.Choice;step=0;}Draw();}
        else if(phase==Phase.ChoiceReply) StartBattle();
        else if(phase==Phase.Ending){step++;if(step>=ending.Count){phase=Phase.Summary;step=0;}Draw();}
    }
    void Choose(int index)
    {
        choiceIndex=index;princeAffinity=Mathf.Clamp(princeAffinity+(index==0?5:index==1?10:-5),0,100);
        phase=Phase.ChoiceReply;Draw();
    }
    void StartBattle()
    {
        heroHP=100;enemyHP=100;hostility=100;focus=0;determination=0;guard=0;exposedTurns=0;skillCooldown=0;turn=1;
        enemyEmotion=Emotion.Furia;
        skillsExpanded = false; itemsExpanded = false;
        battleLog="El príncipe invoca un Demonio Bruto. Observa su FURIA: la diplomacia directa será peligrosa.";
        heroDirection="Right"; battleAttackUntil=0f; enemyAttackUntil=0f;
        phase=Phase.Battle;Draw();
    }

    // HUD V5: estadísticas arriba, personajes libres en el centro y comandos debajo.
    // Los cinco botones mantienen los callbacks originales de la batalla.
    void DrawBattle()
    {
        Picture("Campo del Palacio",stage,palaceWalk,Vector2.zero,Vector2.one,Color.white);
        Panel("Filtro batalla",stage,Vector2.zero,Vector2.one,new Color(.01f,.015f,.07f,.34f));
        Header("BATALLA  /  TURNO " + turn);

        Picture("Heroína sprite usuario",stage,HeroFrame(Time.time < battleAttackUntil ? "Attack_Right" : "Idle_Right", 0) ?? heroArt,
            new Vector2(.105f,.34f),new Vector2(.33f,.77f),Color.white);
        Picture("Demonio sprite usuario",stage,CurrentDemonFrame(),
            new Vector2(.62f,.33f),new Vector2(.89f,.78f),Color.white);
        if (attackBeam != null)
        {
            Picture("Destello ataque",stage,attackBeam,new Vector2(.30f,.46f),new Vector2(.72f,.61f),Color.white);
            var beam=stage.Find("Destello ataque");
            if (beam!=null) { beam.GetComponent<Image>().raycastTarget=false; beam.gameObject.SetActive(Time.time< battleAttackUntil); }
        }
        Label("Contra",stage,Time.time < battleAttackUntil ? "¡IMPACTO!" : "VS",23,gold,new Vector2(.445f,.53f),new Vector2(.565f,.62f),TextAnchor.MiddleCenter,FontStyle.Bold);

        var heroStats=Panel("Estado heroína limpio",stage,new Vector2(.035f,.77f),new Vector2(.415f,.905f),new Color(0.05f,0.08f,0.13f,.86f));
        Panel("Borde heroína", heroStats, new Vector2(0,.94f), Vector2.one, gold);
        PortraitMedallion(heroStats,v10FaceHero,true);
        Label("Nombre heroína",heroStats,"ENVIADA DEL CIELO",17,gold,new Vector2(.26f,.68f),new Vector2(.95f,.94f),style:FontStyle.Bold);
        BattleBar(heroStats,"HeroHP","HP",heroHP/100f,blue,new Vector2(.27f,.40f),new Vector2(.95f,.66f));
        BattleBar(heroStats,"HeroDET","DETERMINACIÓN",determination/5f,new Color32(229,186,102,255),new Vector2(.27f,.17f),new Vector2(.95f,.39f));
        Label("Hero extras",heroStats,"ENFOQUE " + focus + (guard>0?"   ·   ESCUDO ACTIVO":""),13,ivory,new Vector2(.27f,.03f),new Vector2(.95f,.18f));

        var enemyStats=Panel("Estado enemigo limpio",stage,new Vector2(.585f,.77f),new Vector2(.965f,.905f),new Color(0.09f,0.06f,0.08f,.86f));
        Panel("Borde enemigo", enemyStats, new Vector2(0,.94f), Vector2.one, gold);
        PortraitMedallion(enemyStats,v10FaceEnemy,false);
        Label("Nombre enemigo",enemyStats,"DEMONIO INVOCADO  /  FURIA",16,gold,new Vector2(.04f,.68f),new Vector2(.72f,.94f),style:FontStyle.Bold);
        BattleBar(enemyStats,"EnemyHP","HP",enemyHP/100f,red,new Vector2(.04f,.40f),new Vector2(.72f,.66f));
        BattleBar(enemyStats,"EnemyHost","HOSTILIDAD",hostility/100f,new Color32(214,96,118,255),new Vector2(.04f,.17f),new Vector2(.72f,.39f));
        Label("Enemy extras",enemyStats,exposedTurns>0?"VULNERABLE" : "RESISTIENDO",13,ivory,new Vector2(.04f,.03f),new Vector2(.72f,.18f));

        var message=ParchmentPanel("Registro batalla FF",stage,new Vector2(.04f,.05f),new Vector2(.565f,.315f));
        Label("Título registro",message,"REGISTRO DE COMBATE",17,gold,new Vector2(.035f,.77f),new Vector2(.965f,.96f),style:FontStyle.Bold);
        Label("Texto registro",message,battleLog,17,ivory,new Vector2(.035f,.22f),new Vector2(.965f,.74f));
        Label("Ayuda habilidad",message,"SKILL: LANZA ESPIRITUAL / PURIFICAR     ·     ORO: " + totalGold,13,gold,
            new Vector2(.035f,.03f),new Vector2(.965f,.22f));

        var command=ParchmentPanel("Comandos FF",stage,new Vector2(.595f,.05f),new Vector2(.96f,.315f));
        Label("Titulo comandos",command,"¿QUÉ HARÁS?",17,gold,new Vector2(.035f,.79f),new Vector2(.965f,.97f),TextAnchor.MiddleCenter,FontStyle.Bold);
        if (skillsExpanded)
        {
            Label("Habilidades titulo",command,"HABILIDADES   /   SKILL",15,gold,new Vector2(.04f,.78f),new Vector2(.96f,.96f),TextAnchor.MiddleCenter,FontStyle.Bold);
            var spear=Btn("LANZA ESPIRITUAL  (2 DET)",command,new Vector2(.04f,.53f),new Vector2(.96f,.76f),Spear,ffBlue2,13);
            spear.interactable=determination>=2 && skillCooldown==0;
            var purify=Btn("LUZ PURIFICADORA  (2 ENF)",command,new Vector2(.04f,.28f),new Vector2(.96f,.50f),Purify,ffBlue2,13);
            purify.interactable=focus>=2;
            Btn("VOLVER",command,new Vector2(.24f,.035f),new Vector2(.76f,.24f),()=>{skillsExpanded=false;Draw();},new Color32(87,62,101,255),13);
        }
        else if (itemsExpanded)
        {
            Label("Bolsa titulo",command,"OBJETOS   /   ITEMS",15,gold,new Vector2(.04f,.76f),new Vector2(.96f,.96f),TextAnchor.MiddleCenter,FontStyle.Bold);
            Label("Bolsa vacia",command,"BOLSA VACÍA   ·   Próximamente",15,ivory,new Vector2(.04f,.37f),new Vector2(.96f,.68f),TextAnchor.MiddleCenter);
            Btn("VOLVER",command,new Vector2(.24f,.035f),new Vector2(.76f,.25f),()=>{itemsExpanded=false;Draw();},new Color32(87,62,101,255),13);
        }
        else
        {
            Btn("ATACAR",command,new Vector2(.04f,.52f),new Vector2(.48f,.77f),Attack,ffBlue2,16);
            Btn("DEFENDER",command,new Vector2(.52f,.52f),new Vector2(.96f,.77f),Defend,ffBlue2,15);
            Btn("DIPLOMACIA",command,new Vector2(.04f,.20f),new Vector2(.48f,.46f),Diplomacy,ffBlue2,15);
            Btn("ITEMS",command,new Vector2(.52f,.20f),new Vector2(.96f,.46f),()=> { itemsExpanded = true; skillsExpanded = false; Draw(); },ffBlue2,15);
            Btn("SKILL   ▼",command,new Vector2(.27f,.02f),new Vector2(.73f,.195f),()=> { skillsExpanded = true; itemsExpanded = false; Draw(); },new Color32(104,68,111,255),15);
        }
        if(exposedTurns>0) Label("Vulnerable",stage,"¡ENEMIGO VULNERABLE!",16,gold,
            new Vector2(.37f,.66f),new Vector2(.63f,.73f),TextAnchor.MiddleCenter,FontStyle.Bold);
    }
    void Attack()
    {
        battleAttackUntil=Time.time+.48f;
        int damage=exposedTurns>0?27:18; enemyHP=Mathf.Max(0,enemyHP-damage);
        determination=Mathf.Min(5,determination+1);
        PlayerAction("Rayo de Luz causa "+damage+" de daño. +1 determinación.");
    }
    void Defend()
    {
        guard=1; focus=Mathf.Min(5,focus+1);
        // El escudo resiste Furia y prepara una oportunidad pacifica.
        exposedTurns=1;
        PlayerAction("Escudo de Plumas: reduces el siguiente daño. +1 enfoque. El enemigo queda expuesto.");
    }
    void Diplomacy()
    {
        if(focus==0 && exposedTurns==0 && hostility>35)
        {
            hostility=Mathf.Min(100,hostility+10);
            // Castigo explícito del Demonio Bruto en estado de Furia.
            EnemyTurn("Calma Celestial falla ante la Furia: la hostilidad aumenta y el demonio contraataca dos veces.",true);
            return;
        }
        int reduction=30+(exposedTurns>0?10:0);
        hostility=Mathf.Max(0,hostility-reduction);
        PlayerAction("Calma Celestial reduce "+reduction+" puntos de hostilidad.");
    }
    void Spear()
    {
        battleAttackUntil=Time.time+.48f;
        determination-=2;int damage=exposedTurns>0?54:38;
        enemyHP=Mathf.Max(0,enemyHP-damage);skillCooldown=2;
        PlayerAction("Lanza Espiritual atraviesa al enemigo: "+damage+" de daño. Entra en recarga.");
    }
    void Purify()
    {
        focus-=2;hostility=Mathf.Max(0,hostility-75);
        exposedTurns=0;
        PlayerAction("Luz Purificadora disipa el odio: -75 hostilidad. El demonio titubea.");
    }
    void PlayerAction(string result)
    {
        if(enemyHP<=0){Finish(false);return;}
        if(hostility<=0){Finish(true);return;}
        EnemyTurn(result,false);
    }
    void EnemyTurn(string preceding,bool doubleAttack)
    {
        enemyAttackUntil = Time.time + 0.42f;
        int damage=doubleAttack?26:turn%3==0?21:15;
        if(guard>0){damage=Mathf.CeilToInt(damage*.25f); guard=0;}
        heroHP=Mathf.Max(0,heroHP-damage);
        battleLog=preceding+"  |  El Demonio Bruto responde: -"+damage+" HP.";
        if(exposedTurns>0) exposedTurns--;
        if(skillCooldown>0) skillCooldown--;
        turn++;
        if(heroHP<=0) phase=Phase.Defeat;
        Draw();
    }
    void Finish(bool pacific)
    {
        peaceVictory=pacific;
        lastGoldLoot = pacific ? 100 : 150;
        lastLootItem = pacific ? "Emblema de tregua" : "Fragmento infernal";
        totalGold += lastGoldLoot;princeAffinity=Mathf.Clamp(princeAffinity+5,0,100);scholarAffinity=Mathf.Clamp(scholarAffinity+5,0,100);
        ending=pacific?new List<Line> {
            new Line("PRÍNCIPE","¿Convenciste a esa criatura de detenerse con el poder del amor? ¡Ja, ja! Increíble... No eres tan simplona como aparentas.","sorprendido"),
            new Line("ERUDITO","Impresionante, la aureola no la tiene de adorno...", "anota en una libreta"),
            new Line("SISTEMA","El Príncipe y el Erudito han ganado +5 de afinidad. Tu decisión ha demostrado que existe otra forma de detener una guerra.")
        }:new List<Line> {
            new Line("PRÍNCIPE","Vaya... Parece que esas alas sirven para algo más que decorar tu espalda.","impresionado"),
            new Line("ERUDITO","Tiene habilidad, alteza.","toma nota con una pluma"),
            new Line("SISTEMA","El Príncipe y el Erudito han ganado +5 de afinidad. La paz quizá esté un paso más cerca... o más lejos.")
        };
        step=0;phase=Phase.Loot;Draw();
    }
    void DrawLoot()
    {
        BackgroundVictory(); Header("RECOMPENSAS  /  BOTÍN");
        Panel("Velo victoria leve",stage,new Vector2(.24f,.14f),new Vector2(.76f,.82f),new Color(0.02f,0.04f,0.07f,.42f));
        Picture("Príncipe idle victoria",stage,AnimatedPrinceIdleFrame(),new Vector2(.73f,.39f),new Vector2(.83f,.61f),Color.white);
        Picture("Erudito idle victoria",stage,AnimatedScholarIdleFrame(),new Vector2(.18f,.40f),new Vector2(.27f,.58f),Color.white);
        var victoria = Label("Victoria",stage,"¡VICTORIA!",46,gold,new Vector2(.30f,.68f),new Vector2(.70f,.79f),TextAnchor.MiddleCenter,FontStyle.Bold);
        SoftShadow(victoria, new Color(0f,0f,0f,.75f), new Vector2(1.4f,-1.4f));
        if (goldCoin != null) Picture("Moneda de oro",stage,goldCoin,new Vector2(.35f,.49f),new Vector2(.42f,.60f),Color.white);
        var monedas = Label("Monedas",stage,"+"+lastGoldLoot+" MONEDAS DE ORO",28,new Color32(255,216,100,255),new Vector2(.42f,.49f),new Vector2(.70f,.60f),TextAnchor.MiddleLeft,FontStyle.Bold);
        SoftShadow(monedas, new Color(0f,0f,0f,.6f), new Vector2(1f,-1f));
        var botin = Label("Botin especial",stage,"BOTÍN:  "+lastLootItem+"  x1",20,ivory,new Vector2(.30f,.39f),new Vector2(.70f,.47f),TextAnchor.MiddleCenter);
        var oro = Label("Oro total",stage,"ORO ACUMULADO:  "+totalGold,18,ivory,new Vector2(.30f,.31f),new Vector2(.70f,.38f),TextAnchor.MiddleCenter);
        SoftShadow(botin); SoftShadow(oro);
        Btn("RECOGER Y CONTINUAR",stage,new Vector2(.36f,.20f),new Vector2(.64f,.28f),()=> { phase=Phase.Ending;Draw(); },ffBlue2,18);
    }
    void DrawSummary()
    {
        BackgroundVictory();Header("FIN DE LA DEMO");
        var title = Label("Titulo final",stage,"LA TREGUA IMPOSIBLE",34,gold,new Vector2(.25f,.69f),new Vector2(.75f,.79f),TextAnchor.MiddleCenter,FontStyle.Bold);
        var type = Label("Tipo victoria",stage,peaceVictory?"FINAL: VICTORIA PACÍFICA":"FINAL: VICTORIA POR COMBATE",23,
            peaceVictory?green:red,new Vector2(.25f,.57f),new Vector2(.75f,.66f),TextAnchor.MiddleCenter,FontStyle.Bold);
        var princeTxt = Label("Relacion Principe",stage,"AFINIDAD CON EL PRÍNCIPE    "+princeAffinity+"%",19,ivory,new Vector2(.27f,.45f),new Vector2(.73f,.54f),TextAnchor.MiddleCenter);
        var scholarTxt = Label("Relacion Erudito",stage,"AFINIDAD CON EL ERUDITO    "+scholarAffinity+"%",19,ivory,new Vector2(.27f,.35f),new Vector2(.73f,.44f),TextAnchor.MiddleCenter);
        var epi = Label("Epílogo",stage,"ORO OBTENIDO: " + totalGold + "    ·    BOTÍN: " + lastLootItem + "\nTus decisiones alteran los vínculos y el destino.",18,ivory,
            new Vector2(.25f,.235f),new Vector2(.75f,.345f),TextAnchor.MiddleCenter);
        SoftShadow(title, new Color(0f,0f,0f,.72f), new Vector2(1.3f,-1.3f));
        SoftShadow(type); SoftShadow(princeTxt); SoftShadow(scholarTxt); SoftShadow(epi);
        Picture("Príncipe idle resumen",stage,AnimatedPrinceIdleFrame(),new Vector2(.66f,.19f),new Vector2(.76f,.39f),Color.white);
        Picture("Erudito idle resumen",stage,AnimatedScholarIdleFrame(),new Vector2(.24f,.19f),new Vector2(.33f,.37f),Color.white);
        Btn("VOLVER A JUGAR",stage,new Vector2(.35f,.15f),new Vector2(.65f,.225f),StartAgain);
        Btn("MENÚ PRINCIPAL",stage,new Vector2(.35f,.055f),new Vector2(.65f,.13f),OpenMenu);
    }
}
