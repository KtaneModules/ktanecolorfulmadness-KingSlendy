using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KMBombInfoHelper;
using UnityEngine;

using Random = UnityEngine.Random;

public class ColorfulMadnessScript : MonoBehaviour {
	public KMSelectable[] buttons;
	public KMBombInfo Info;

	public Texture2D[] colTexturesA = new Texture2D [1];
	public Texture2D[] colTexturesB = new Texture2D [1];
	int[] topHalfTextures = new int [10];
	int[] bottomHalfTextures = new int [10];

	int[] digits = new int [3];
	bool hasRY;
	int numCheckerboards;
	bool hasSquare;

	List<int> pressedButtons = new List<int> ();

	static int moduleIdCounter = 1;
	int moduleId;

	void Start () {
		moduleId = moduleIdCounter++;

		var serialNum = Info.GetSerialNumber ();

		for (int i = 0; i < 3; i++) {
			digits [i] = serialNum [i * 2];

			if (digits [i] >= 'A') {
				digits [i] -= 'A';
			} else {
				digits [i] -= '0';
			}
		}

		Debug.LogFormat (@"[Colorful Madness #{0}] The three values from the serial number are: {1}, {2}, {3}", moduleId, digits [0], digits [1], digits [2]);

		// Set up the top half of the board
		for (int i = 0; i < 10; i++) {
			do {
				topHalfTextures [i] = Random.Range (0, 105);
			} while (bottomHalfTextures.Contains (topHalfTextures [i]));

			if (topHalfTextures [i] <= 6) {
				hasRY = true;
			}

			if ((topHalfTextures [i] % 7) == 4) {
				numCheckerboards++;
			}

			if ((topHalfTextures [i] % 7) == 3) {
				hasSquare = true;
			}

			buttons [i].transform.GetChild (1).GetComponent<Renderer> ().material.SetTexture ("_MainTex", colTexturesA [topHalfTextures [i]]);
			bottomHalfTextures [i] = topHalfTextures [i];
		}

		for (int v = 0; v < bottomHalfTextures.Length; v++) {
			int tmp = bottomHalfTextures [v];
			int r = Random.Range (v, bottomHalfTextures.Length);
			bottomHalfTextures [v] = bottomHalfTextures [r];
			bottomHalfTextures [r] = tmp;
		}

		for (int i = 10; i < 20; i++) {
			buttons [i].transform.GetChild (1).GetComponent<Renderer> ().material.SetTexture ("_MainTex", colTexturesB [bottomHalfTextures [i - 10]]);
		}

		if (hasRY) {
			var batteryNum = Info.GetBatteryCount ();

			for (int i = 0; i < 3; i++) {
				digits [i] += batteryNum;
			}

			Debug.LogFormat (@"[Colorful Madness #{0}] There is a red-and-yellow button. Adding {4}: {1}, {2}, {3}", moduleId, digits [0], digits [1], digits [2], batteryNum);
		}

		if (numCheckerboards > 0) {
			var portNum = Info.GetPortCount ();
			var value = Mathf.Abs ((numCheckerboards * 2) - portNum);

			for (int i = 0; i < 3; i++) {
				digits [i] = Mathf.Abs (digits [i] - value);
			}

			Debug.LogFormat (@"[Colorful Madness #{0}] There are {5} checkerboard buttons. Subtracting {4}: {1}, {2}, {3}", moduleId, digits [0], digits [1], digits [2], value, 2 * numCheckerboards);
		}

		if (hasSquare) {
			var holdersPlusPortPlates = Info.GetBatteryHolderCount () + Info.GetPortPlateCount ();

			for (int i = 0; i < 3; i++) {
				digits [i] *= holdersPlusPortPlates;
			}

			Debug.LogFormat (@"[Colorful Madness #{0}] There is a square-on-square button. Multiplying by {4}: {1}, {2}, {3}", moduleId, digits [0], digits [1], digits [2], holdersPlusPortPlates);
		}

		for (int i = 0; i < 3; i++) {
			digits [i] = digits [i] % 10;
		}

		Debug.LogFormat (@"[Colorful Madness #{0}] Modulo 10: {1}, {2}, {3}", moduleId, digits [0], digits [1], digits [2]);

		while (digits [0] == digits [1] || digits [0] == digits [2]) {
			digits [0] = (digits [0] + 1) % 10;
		}

		while (digits [1] == digits [0] || digits [1] == digits [2]) {
			digits [1] = (digits [1] + 9) % 10;
		}

		Debug.LogFormat (@"[Colorful Madness #{0}] Make unique: {1}, {2}, {3}", moduleId, digits [0], digits [1], digits [2]);
		Debug.LogFormat (@"[Colorful Madness #{0}] Added one: {1}, {2}, {3}", moduleId, digits [0] + 1, digits [1] + 1, digits [2] + 1);

		var counterparts = digits.Select (digit => Array.IndexOf (bottomHalfTextures, topHalfTextures [digit]) + 10).ToArray ();
		Debug.LogFormat (@"[Colorful Madness #{0}] The correct counterpart buttons on the bottom half are: {1}, {2}, {3}", moduleId, counterparts [0] + 1, counterparts [1] + 1, counterparts [2] + 1);

		for (int i = 0; i < buttons.Length; i++) {
			int j = i;

			buttons [i].OnInteract += delegate () {
				onPress (j);

				return false;
			};
		}
	}

	void onPress (int pressed) {
		GetComponent<KMAudio> ().PlayGameSoundAtTransform (KMSoundOverride.SoundEffect.ButtonPress, transform);
		GetComponent<KMSelectable> ().AddInteractionPunch ();

		for (int p = 0; p < pressedButtons.Count; p++) {
			if (pressed == pressedButtons [p]) {
				// Correct button already pressed before: ignore
				return;
			}
		}

		if (
			// Pressed any of the correct buttons in the top half
			digits.Contains (pressed) ||
			// Pressed any of the correct counterparts in the bottom half
			(pressed >= 10 && digits.Any (digit => bottomHalfTextures [pressed - 10] == topHalfTextures [digit]))
		) {
			pressedButtons.Add (pressed);
			Debug.LogFormat (@"[Colorful Madness #{0}] You pressed {1}, which is correct. {2} more to go.", moduleId, pressed + 1, 6 - pressedButtons.Count);

			if (pressedButtons.Count == 6) {
				Debug.LogFormat (@"[Colorful Madness #{0}] Module solved.", moduleId);
				GetComponent<KMBombModule> ().HandlePass ();
			}
		} else {
			Debug.LogFormat (@"[Colorful Madness #{0}] You pressed {1}, which is incorrect.", moduleId, pressed + 1);
			GetComponent<KMBombModule> ().HandleStrike ();
		}
	}

	#pragma warning disable 414
		private readonly string TwitchHelpMessage = @"!{0} press 1 2 3... (buttons to be pressed [within the range 1-20])";
	#pragma warning restore 414

	KMSelectable[] ProcessTwitchCommand (string command) {
		command = command.ToLowerInvariant ().Trim ();

		if (Regex.IsMatch (command, @"^press +[0-9^, |&]{1,}$")) {
			command = command.Substring (6).Trim ();

			var presses = command.Split (new [] { ',', ' ', '|', '&' }, System.StringSplitOptions.RemoveEmptyEntries);
			var pressList = new List<KMSelectable> ();

			for (int i = 0; i < presses.Length; i++) {
				if (Regex.IsMatch (presses [i], @"^[0-9]{1,2}$")) {
					pressList.Add (buttons [Mathf.Clamp (Math.Max (0, int.Parse (presses [i].ToString ()) - 1), 0, buttons.Length - 1)]);
				}
			}

			return pressList.ToArray ();
		}

		return null;
	}
}