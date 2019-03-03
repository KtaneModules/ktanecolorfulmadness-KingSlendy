using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KMBombInfoHelper;
using UnityEngine;

using Random = UnityEngine.Random;
using Math = UnityEngine.Mathf;

//Thanks to Timwi for improving my previous mess of code
public class ColorfulMadnessScript : MonoBehaviour {
	public KMAudio BombAudio;
	public KMBombInfo BombInfo;
	public KMBombModule BombModule;
	public KMSelectable[] ModuleButtons;
	public KMSelectable ModuleSelect;
	public KMRuleSeedable RuleSeedable;
	public KMColorblindMode ColorblindMode;
	public Texture2D[] ColTexturesA = new Texture2D[1];
	public Texture2D[] ColTexturesB = new Texture2D[1];
	public GameObject ColScreen;

	readonly int[] topHalfTextures = new int[10];
	int[] bottomHalfTextures = new int[10];
	int[] moduleTextures = new int[20];
	MonoRandom rnd;
	List<int> pickedValues = new List<int>();
	int[] digits = new int[3];
	readonly string[] moduleButtonNames = {
		"vertical half-half",
		"horizontal half-half",
		"2x2 checkerboard",
		"square-on-square",
		"4x4 checkerboard",
		"vertical big-stripe",
		"horizontal big-stripe"
	};

	readonly string[] moduleColors = { "red", "yellow", "green", "cyan", "blue", "purple" };
	delegate bool checkCol(int x, int y, int z);
	checkCol getCol;
	List<int> pressedButtons = new List<int>();

	static int moduleIdCounter = 1;
	int moduleId;

    void Start() {
        moduleId = moduleIdCounter++;
        var serialNum = BombInfo.GetSerialNumber();
        rnd = RuleSeedable.GetRNG();
        Debug.LogFormat(@"[Colorful Madness #{0}] Using rule seed: {1}", moduleId, rnd.Seed);
        var grabSerial = new[] { 0, 2, 4 };
        var firstColor = 0;
        var secondColor = 1;
        var modSteps = new[] { 4, 0, 5, 6, 7 };
        var firstPattern = 4;
        var secondPattern = 3;
        var firstUnique = 1;
        var secondUnique = 1;

        if (rnd.Seed != 1) {
            firstColor = rnd.Next(5);
            secondColor = rnd.Next(firstColor + 1, 6);
            grabSerial = new[] { ChooseUnique(6), ChooseUnique(6), ChooseUnique(6) };
            pickedValues.Clear();
            modSteps = new[] { ChooseUnique(10), ChooseUnique(10), ChooseUnique(10), ChooseUnique(10), ChooseUnique(10) };
            pickedValues.Clear();
            firstPattern = ChooseUnique(7);
            secondPattern = ChooseUnique(7);
            pickedValues.Clear();
            firstUnique = rnd.Next(1, 5);
            secondUnique = rnd.Next(1, 5);
        }

        var subCols = new[] { 0, 0, 1, 3, 6, 10 };
        var initCol = (7 * ((5 * firstColor) - subCols[firstColor])) + (7 * (secondColor - (firstColor + 1)));
        ColScreen.SetActive(ColorblindMode.ColorblindModeActive);

        for (int i = 0; i < 3; i++) {
            digits[i] = serialNum[grabSerial[i]];

            if (digits[i] >= 'A') {
                digits[i] -= 'A';
            } else {
                digits[i] -= '0';
            }
        }
        
        Debug.LogFormat(@"[Colorful Madness #{0}] The 3 characters of the serial number are: {1}", moduleId, Enumerable.Range(0, 3).Select(x => serialNum[grabSerial[x]]).Join(", "));
		Debug.LogFormat(@"[Colorful Madness #{0}] The 3 main digits are: {1}", moduleId, digits.Join(", "));
		var hasButtonCC = 0;
		var hasButtonA = 0;
		var hasButtonB = 0;

		for (int i = 0; i < 10; i++) {
			do {
				topHalfTextures[i] = Random.Range(0, 105);
			} while (bottomHalfTextures.Contains(topHalfTextures[i]));

			if (topHalfTextures[i] >= initCol && topHalfTextures[i] <= initCol + 6) {
				hasButtonCC++;
			}

			if ((topHalfTextures[i] % 7) == firstPattern) {
				hasButtonA++;
			}

			if ((topHalfTextures[i] % 7) == secondPattern) {
				hasButtonB++;
			}

			ModuleButtons[i].transform.GetChild(1).GetComponent<Renderer>().material.SetTexture("_MainTex", ColTexturesA[topHalfTextures[i]]);
			bottomHalfTextures[i] = topHalfTextures[i];
		}

		for (int v = 0; v < bottomHalfTextures.Length; v++) {
			var tmp = bottomHalfTextures[v];
			var r = Random.Range(v, bottomHalfTextures.Length);
			bottomHalfTextures[v] = bottomHalfTextures[r];
			bottomHalfTextures[r] = tmp;
		}

		for (int i = 10; i < 20; i++) {
			ModuleButtons[i].transform.GetChild(1).GetComponent<Renderer>().material.SetTexture("_MainTex", ColTexturesB[bottomHalfTextures[i - 10]]);
		}

		var tempList = new List<int>();
		tempList.AddRange(topHalfTextures);
		tempList.AddRange(bottomHalfTextures);
		moduleTextures = tempList.ToArray();
		getCol = ((x, y, z) => ((7 * ((5 * x) - subCols[x])) + (7 * (y - (x + 1))) == (z - (z % 7))));

		var serialDigits = BombInfo.GetSerialNumberNumbers().ToArray();
		var stepList = new[] {
			hasButtonCC * 2,
			digits.Sum(),
			serialDigits.Sum(),
			int.Parse(serialNum[5].ToString()),
			BombInfo.GetBatteryCount(),
			BombInfo.GetPortCount(),
			BombInfo.GetBatteryHolderCount(),
			BombInfo.GetPortPlateCount(),
			Math.Min(serialDigits),
			Math.Max(serialDigits)
		};

		var modValue = 0;

		if (hasButtonCC > 0) {
			modValue = stepList[modSteps[0]];

			for (int i = 0; i < 3; i++) {
				digits[i] += modValue;
			}

			Debug.LogFormat(@"[Colorful Madness #{0}] There are {1} {2} and {3} button. Adding {4}: {5}", moduleId, hasButtonCC * 2, moduleColors[firstColor], moduleColors[secondColor], modValue, digits.Join(", "));
		}

		stepList[0] = hasButtonA * 2;
		stepList[1] = digits.Sum();

		if (hasButtonA > 0) {
			modValue = Mathf.Abs(stepList[modSteps[1]] - stepList[modSteps[2]]);

			for (int i = 0; i < 3; i++) {
				digits[i] = Mathf.Abs(digits[i] - modValue);
			}

			Debug.LogFormat(@"[Colorful Madness #{0}] There are {1} {2} buttons. Subtracting {3}: {4}", moduleId, hasButtonA * 2, moduleButtonNames[firstPattern], modValue, digits.Join(", "));
		}

		stepList[0] = hasButtonB * 2;
		stepList[1] = digits.Sum();

		if (hasButtonB > 0) {
			modValue = stepList[modSteps[3]] + stepList[modSteps[4]];

			for (int i = 0; i < 3; i++) {
				digits[i] *= modValue;
			}

			Debug.LogFormat(@"[Colorful Madness #{0}] There are {1} {2} buttons. Multiplying by {3}: {4}", moduleId, hasButtonB * 2, moduleButtonNames[secondPattern], modValue, digits.Join(", "));
		}

		for (int i = 0; i < 3; i++) {
			digits[i] = digits[i] % 10;
		}

		Debug.LogFormat(@"[Colorful Madness #{0}] Modulo 10: {1}", moduleId, digits.Join(", "));

		while (digits[0] == digits[1] || digits[0] == digits[2]) {
			digits[0] = (digits[0] + firstUnique) % 10;
		}

		while (digits[1] == digits[0] || digits[1] == digits[2]) {
			digits[1] = (digits[1] + (10 - secondUnique)) % 10;
		}

		Debug.LogFormat(@"[Colorful Madness #{0}] Make unique: {1}", moduleId, digits.Join(", "));
		Debug.LogFormat(@"[Colorful Madness #{0}] Added one: {1}", moduleId, digits.Select(x => x + 1).Join(", "));

		var counterparts = digits.Select(digit => Array.IndexOf(bottomHalfTextures, topHalfTextures[digit]) + 10).ToArray();
		Debug.LogFormat(@"[Colorful Madness #{0}] The correct counterpart buttons on the bottom half are: {1}", moduleId, counterparts.Select(x => x + 1).Join(", "));

		for (int i = 0; i < ModuleButtons.Length; i++) {
			int j = i;

			ModuleButtons[i].OnInteract += delegate() {
				OnButtonPress(j);

				return false;
			};

			ModuleButtons[i].OnHighlight += delegate() {
				OnButtonHighlight(j);
			};

			ModuleButtons[i].OnDeselect += delegate() {
				ColScreen.transform.GetChild(0).GetComponent<TextMesh>().text = "";
			};
		}
	}

	int ChooseUnique(int maxVal) {
		var nowPicked = 0;

		do {
			nowPicked = rnd.Next(maxVal);
		} while (pickedValues.Contains(nowPicked));

		pickedValues.Add(nowPicked);

		return nowPicked;
	}

	void OnButtonPress(int pressed) {
		BombAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
		ModuleSelect.AddInteractionPunch();

		for (int p = 0; p < pressedButtons.Count; p++) {
			if (pressed == pressedButtons[p]) {
				return;
			}
		}

        var buttonComp = ModuleButtons[pressed].GetComponent<Renderer>();

		if (digits.Contains(pressed) || (pressed >= 10 && digits.Any(digit => bottomHalfTextures[pressed - 10] == topHalfTextures[digit]))) {
			pressedButtons.Add(pressed);
            buttonComp.material.color = Color.white;
			Debug.LogFormat(@"[Colorful Madness #{0}] You pressed {1}, which is correct. {2} more to go.", moduleId, pressed + 1, 6 - pressedButtons.Count);

			if (pressedButtons.Count == 6) {
				Debug.LogFormat(@"[Colorful Madness #{0}] Module solved!", moduleId);
				BombModule.HandlePass();
			}
		} else {
            buttonComp.material.color = Color.gray;
			Debug.LogFormat(@"[Colorful Madness #{0}] You pressed {1}, which is incorrect.", moduleId, pressed + 1);
			BombModule.HandleStrike();
		}
	}

	void OnButtonHighlight(int highlighted) {
		if (!ColScreen.activeSelf) {
			return;
		}

		var compText = ColScreen.transform.GetChild(0).GetComponent<TextMesh>();
		compText.text = "";
		compText.text += moduleButtonNames[moduleTextures[highlighted] % 7] + "\n";

		for (int i = 0; i < 5; i++) {
			for (int j = 1; j < 6; j++) {
				if (getCol(i, j, moduleTextures[highlighted])) {
					compText.text += (highlighted < 10) ? moduleColors[i] + "-" + moduleColors[j] : moduleColors[j] + "-" + moduleColors[i];
					i = 4;

					break;
				}
			}
		}
	}

	IEnumerator ShowScreen(List<int> passedShow) {
		var nowShow = 0;

		while (nowShow < passedShow.Count) {
			OnButtonHighlight(passedShow[nowShow++]);

			yield return new WaitForSeconds(2.0f);
		}

		ColScreen.transform.GetChild(0).GetComponent<TextMesh>().text = "";
	}

	#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"!{0} press 1 2 3... (buttons to be pressed [within the range 1-20]) | !{0} show all/top/bottom / 1 2 3... (displays the buttons' info on the screen [all/top/bottom show the respective half of buttons] [numbers are within the range 1-20] [only works with colorblind mode enabled]) | !{0} colorblind/cb (enables colorblind mode)";
	#pragma warning restore 414

	KMSelectable[] ProcessTwitchCommand(string command) {
		command = command.ToLowerInvariant().Trim();

		if (Regex.IsMatch(command, @"^press +[0-9^, |&]+$")) {
			command = command.Substring(6).Trim();
			var presses = command.Split(new[] { ',', ' ', '|', '&' }, StringSplitOptions.RemoveEmptyEntries);
			var pressList = new List<KMSelectable>();

			for (int i = 0; i < presses.Length; i++) {
				if (Regex.IsMatch(presses[i], @"^[0-9]{1,2}$")) {
					pressList.Add(ModuleButtons[Math.Clamp(Math.Max(1, int.Parse(presses[i].ToString())) - 1, 0, ModuleButtons.Length - 1)]);
				}
			}

			return (pressList.Count > 0) ? pressList.ToArray() : null;
		}

		if (Regex.IsMatch(command, @"^show +([0-9^, |&]|all|top|bottom)+$")) {
			if (!ColScreen.activeSelf) {
				return null;
			}

			command = command.Substring(5).Trim();
			var showing = command.Split(new[] { ',', ' ', '|', '&' }, StringSplitOptions.RemoveEmptyEntries);
			var showList = new List<int>();

			for (int i = 0; i < showing.Length; i++) {
				if (Regex.IsMatch(showing[i], @"^[0-9]{1,2}$")) {
					showList.Add(Math.Clamp(Math.Max(1, int.Parse(showing[i].ToString())) - 1, 0, ModuleButtons.Length - 1));
				} else {
					showList.Clear();

					for (int j = ((showing[i].Equals("bottom")) ? 10 : 0); j < 20 - (((showing[i].Equals("top")) ? 10 : 0)); j++) {
						showList.Add(j);
					}

					break;
				}
			}

			if (showList.Count == 0) {
				return null;
			}

			StopAllCoroutines();
			StartCoroutine(ShowScreen(showList));

			return new KMSelectable[0];
		}

		if (Regex.IsMatch(command, @"^\s*(colorblind|cb)\s*$")) {
			ColScreen.SetActive(true);

			return new KMSelectable[0];
		}

		return null;
	}
}