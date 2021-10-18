using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using rnd = UnityEngine.Random;

public class marcoPolo : MonoBehaviour
{
    public new KMAudio audio;
	public AudioSource beepPlayer;
    public KMBombInfo bomb;
	public KMBombModule module;

	public KMSelectable[] buttons;
	public KMSelectable soundButton;
	public TextMesh[] buttonTexts;
	public Renderer[] leds;
	public Color[] textColors;
	public Color on;
	public Color off;
	public Color solvedColor;
	public Color strikeColor;

	private int stage;
	private bool[] isBlue = new bool[3];
	private int[] directionIndices = new int[3];
	private int[] solution = new int[3];
	private int[][] labelOrders = new int[3][];

	private bool cantPress;
	private bool perfect = true;
	private static readonly string[] labels = new string[7] { "FL", "FM", "MR", "BR", "BM", "BL", "ML" };
	private static readonly string[] directionNames = new string[7] { "front-left", "front-middle", "middle-right", "back-right", "back-middle", "back-left", "middle-left" };

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;

    void Awake()
    {
    	moduleId = moduleIdCounter++;
		foreach (KMSelectable button in buttons)
			button.OnInteract += delegate () { PressButton(button); return false; };
		soundButton.OnInteract += delegate () { StartCoroutine(PressSoundButton()); return false; };
    }

    void Start()
    {
		for (int i = 0; i < 3; i++)
		{
			isBlue[i] = rnd.Range(0,2) == 0;
			directionIndices[i] = rnd.Range(0,7);
			labelOrders[i] = Enumerable.Range(0,7).ToList().Shuffle().ToArray();
			Debug.LogFormat("[Marco Polo #{0}] Stage {1}:", moduleId, i + 1);
			if (isBlue[i])
			{
				solution[i] = Array.IndexOf(labelOrders[i], directionIndices[i]);
				Debug.LogFormat("[Marco Polo #{0}] The text is blue, and the sound is coming from the {1}, so the correct button to press is the one with that label.", moduleId, directionNames[directionIndices[i]]);
			}
			else
			{
				solution[i] = directionIndices[i];
				Debug.LogFormat("[Marco Polo #{0}] The text is black, and the sound is coming from the {1}, so the correct button to press is the one in that position.", moduleId, directionNames[solution[i]]);
			}
		}
		StartCoroutine(UpdateButtons());
    }

	IEnumerator UpdateButtons()
	{
		cantPress = true;
		if (stage != 0)
		{
			for (int i = 0; i < 7; i++)
			{
				yield return new WaitForSeconds(.3f);
				audio.PlaySoundAtTransform("tap", buttons[i].transform);
				buttonTexts[i].text = "";
			}
			yield return new WaitForSeconds(.4f);
			for (int i = 0; i < 7; i++)
			{
				if (i != 0)
					yield return new WaitForSeconds(.3f);
				audio.PlaySoundAtTransform("tap", buttons[i].transform);
				buttonTexts[i].text = labels[labelOrders[stage][i]];
				buttonTexts[i].color = !isBlue[stage] ? textColors[0] : textColors[1];
			}
		}
		else
		{
			for (int i = 0; i < 7; i++)
			{
				buttonTexts[i].text = labels[labelOrders[stage][i]];
				buttonTexts[i].color = !isBlue[stage] ? textColors[0] : textColors[1];
			}
		}
		cantPress = false;
	}

	IEnumerator PressSoundButton()
	{
		cantPress = true;
		soundButton.AddInteractionPunch(.5f);
		if (beepPlayer.isPlaying)
			beepPlayer.Stop();
        if (moduleSolved)
        {
            int rando = rnd.Range(0, 7);
			SetBeepPlayer(labels[rando]);
        }
        else
        {
			SetBeepPlayer(labels[directionIndices[stage]]);
		}
		beepPlayer.Play();
		yield return new WaitForSeconds(.75f);
		cantPress = false;
	}

	void SetBeepPlayer(string pos)
    {
		switch (pos)
        {
			case "FL":
				beepPlayer.panStereo = -.5f;
				beepPlayer.volume = .5f;
				break;
			case "FM":
				beepPlayer.panStereo = 0f;
				beepPlayer.volume = .5f;
				break;
			case "ML":
				beepPlayer.panStereo = -1f;
				beepPlayer.volume = .5f;
				break;
			case "MR":
				beepPlayer.panStereo = 1f;
				beepPlayer.volume = .5f;
				break;
			case "BL":
				beepPlayer.panStereo = -.5f;
				beepPlayer.volume = .25f;
				break;
			case "BM":
				beepPlayer.panStereo = 0f;
				beepPlayer.volume = .25f;
				break;
			case "BR":
				beepPlayer.panStereo = .5f;
				beepPlayer.volume = .25f;
				break;
		}
    }

	void PressButton(KMSelectable button)
	{
		button.AddInteractionPunch(.5f);
		audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
		if (moduleSolved || cantPress)
			return;
		var ix = Array.IndexOf(buttons, button);
		if (solution[stage] != ix)
		{
			module.HandleStrike();
			Debug.LogFormat("[Marco Polo #{0}] You pressed the button in the {1} position. That is incorrect. Strike!", moduleId, directionNames[ix]);
			StartCoroutine(FlashLeds());
			perfect = false;
		}
		else
		{
			leds[stage].material.color = on;
			stage++;
			Debug.LogFormat("[Marco Polo #{0}] You pressed the button in the {1} position. That is correct.", moduleId, directionNames[ix]);
			if (stage != 3)
				StartCoroutine(UpdateButtons());
			else
			{
				moduleSolved = true;
                cantPress = true;
				StartCoroutine(Solve());
				Debug.LogFormat("[Marco Polo #{0}] Module solved.", moduleId);
			}
		}
	}

	IEnumerator Solve()
	{
		string[] solvedMessage = perfect ? new string[7] { "P", "E", "R", "F", "E", "C", "T" } : new string[7] { "N", "I", "C", "E", "O", "N", "E" };
		int[] order = new int[7] { 0, 1, 6, 2, 5, 4, 3 };
		for (int i = 0; i < 7; i++)
		{
			yield return new WaitForSeconds(.3f);
			buttonTexts[i].text = "";
			audio.PlaySoundAtTransform("tap", buttons[i].transform);
			buttonTexts[i].color = perfect ? solvedColor : textColors[0];
		}
		yield return new WaitForSeconds(.4f);
		for (int i = 0; i < 7; i++)
		{
			if (i != 0)
				yield return new WaitForSeconds(.3f);
			buttonTexts[order[i]].text = solvedMessage[i];
			audio.PlaySoundAtTransform("tap", buttons[order[i]].transform);
		}
		StartCoroutine(FlashLeds());
		module.HandlePass();
		audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
	}

	IEnumerator FlashLeds()
	{
		cantPress = true;
		for (int i = 0; i < 3; i++)
		{
			if (i != 0)
				yield return new WaitForSeconds(.2f);
			foreach (Renderer led in leds)
				led.material.color = off;
			yield return new WaitForSeconds(.2f);
			foreach (Renderer led in leds)
				led.material.color = moduleSolved ? on : strikeColor;
		}
		if (!moduleSolved)
		{
			for (int i = 0; i < 3; i++)
				if (i < stage)
					leds[i].material.color = on;
				else
					leds[i].material.color = off;
		}
		cantPress = false;
	}

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press <pos> (#) [Presses the button in the specified position (center button only: optionally press '#' times with delays inbetween each press)] | Valid positions are TL, TM, MR, BR, BM, BL, ML, and C (Center)";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*press\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if (parameters.Length == 3)
            {
                string[] valids3s = { "center", "c" };
                parameters[1] = parameters[1].ToLower();
                if (valids3s.Contains(parameters[1]))
                {
                    int temp = 0;
                    bool check = int.TryParse(parameters[2], out temp);
                    if (check)
                    {
                        if (cantPress)
                        {
                            yield return "sendtochaterror Buttons cannot be pressed while the module is animating!";
                            yield break;
                        }
                        int counter = 0;
                        while (counter < temp)
                        {
                            counter++;
                            yield return "trycancel Halting pressing the center button '" + parameters[2] + "' times due to a request to cancel.";
                            soundButton.OnInteract();
                            yield return new WaitForSeconds(1f);
                        }
                    }
                    else
                    {
                        yield return "sendtochaterror The specified number of times to press the center button '" + parameters[2] + "' is invalid!";
                    }
                }
                else
                {
                    yield return "sendtochaterror Only the center button can be pressed a certain number of times!";
                }
            }
            else if (parameters.Length == 2)
            {
                string[] valids = { "top-left", "topleft", "tl", "top-middle", "topmiddle", "tm", "mid-right", "midright", "middle-right", "middleright", "mr", "bottom-right", "bottomright", "br", "bottom-middle", "bottommiddle", "bm", "bottom-left", "bottomleft", "bl", "mid-left", "midleft", "middle-left", "middleleft", "ml", "center", "c" };
                parameters[1] = parameters[1].ToLower();
                if (valids.Contains(parameters[1]))
                {
                    if (cantPress)
                    {
                        yield return "sendtochaterror Buttons cannot be pressed while the module is animating!";
                        yield break;
                    }
                    if (parameters[1].Equals("top-left") || parameters[1].Equals("topleft") || parameters[1].Equals("tl"))
                    {
                        buttons[0].OnInteract();
                    }
                    else if (parameters[1].Equals("top-middle") || parameters[1].Equals("topmiddle") || parameters[1].Equals("tm"))
                    {
                        buttons[1].OnInteract();
                    }
                    else if (parameters[1].Equals("mid-right") || parameters[1].Equals("midright") || parameters[1].Equals("middle-right") || parameters[1].Equals("middleright") || parameters[1].Equals("mr"))
                    {
                        buttons[2].OnInteract();
                    }
                    else if (parameters[1].Equals("bottom-right") || parameters[1].Equals("bottomright") || parameters[1].Equals("br"))
                    {
                        buttons[3].OnInteract();
                    }
                    else if (parameters[1].Equals("bottom-middle") || parameters[1].Equals("bottommiddle") || parameters[1].Equals("bm"))
                    {
                        buttons[4].OnInteract();
                    }
                    else if (parameters[1].Equals("bottom-left") || parameters[1].Equals("bottomleft") || parameters[1].Equals("bl"))
                    {
                        buttons[5].OnInteract();
                    }
                    else if (parameters[1].Equals("mid-left") || parameters[1].Equals("midleft") || parameters[1].Equals("middle-left") || parameters[1].Equals("middleleft") || parameters[1].Equals("ml"))
                    {
                        buttons[6].OnInteract();
                    }
                    else if (parameters[1].Equals("center") || parameters[1].Equals("c"))
                    {
                        soundButton.OnInteract();
                    }
                    if (stage == 3)
                    {
                        yield return "solve";
                    }
                }
                else
                {
                    yield return "sendtochaterror The specified position '" + parameters[1] + "' is invalid!";
                }
            }
            else if (parameters.Length == 1)
            {
                yield return "sendtochaterror Please specify which button to press!";
            }
            else if (parameters.Length > 3)
            {
                yield return "sendtochaterror Too many parameters!";
            }
            yield break;
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (cantPress) { yield return true; }
        for (int i = stage; i < 3; i++)
        {
            buttons[solution[i]].OnInteract();
            while (cantPress) { yield return true; }
        }
    }
}