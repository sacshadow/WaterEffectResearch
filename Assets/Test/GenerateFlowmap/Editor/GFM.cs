using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

public class GFM : EditorWindow {
	private string[] dir = new string[]{
		"↖","↑","↗",
		"←","▣","→",
		"↙","↓","↘",
	};
	
	private int cell = 8;
	
	private int[] flowDir;
	private string[] flowDirText;
	
	private int fmIndex = 0;
	
	[MenuItem("WaterFlow/Generate Flow Map")]
	public static void OpenWindow() {
		GetWindow(typeof(GFM));
	}
	
	void OnEnable() {
		flowDir = new int[cell*cell];
		flowDirText = new string[cell*cell];
		for(int i=0; i<flowDir.Length; i++) flowDir[i] = 4;
		for(int i=0; i<flowDirText.Length; i++) flowDirText[i] = "▣";
	}
	
	void OnGUI() {
		fmIndex = GUILayout.SelectionGrid(fmIndex, flowDirText, cell, GUILayout.Width(cell * 26));
		
		GUILayout.Space(20);
		
		BtnLayout(0,1,2);
		BtnLayout(3,4,5);
		BtnLayout(6,7,8);
		
		GUILayout.FlexibleSpace();
		
		if(GUILayout.Button("Rand"))
			RandMap();
		
		if(GUILayout.Button("Generate"))
			GenerateMap();
	}
	
	private void BtnLayout(int btn0, int btn1, int btn2) {
		GUILayout.BeginHorizontal();
		Btn(btn0);Btn(btn1);Btn(btn2);
		GUILayout.EndHorizontal();
	}
	
	private void Btn(int index) {
		if(GUILayout.Button(dir[index]))
			SetFlowDir(index, fmIndex);
	}
	
	private void SetFlowDir(int index, int fm) {
		flowDir[fm] = index;
		flowDirText[fm] = dir[index];
	}
	
	private void RandMap() {
		var c = dir.Length;
		var k = dir.Length * 10;
		for(int i=0; i<flowDir.Length; i++) SetFlowDir(Random.Range(0,k)%c, i);
	}
	
	private void GenerateMap() {
		var tex = new Texture2D(cell, cell);
		tex.SetPixels(Dir2Pix());
		var b = tex.EncodeToPNG();
		Object.DestroyImmediate(tex);
		
		File.WriteAllBytes(Application.dataPath + "/GFM.png", b);
		AssetDatabase.Refresh();
	}
	
	private Color[] Dir2Pix() {
		var rt = new Color[cell*cell];
		
		var c = new Color[]{
			DirCol(-1,1),DirCol(0,1),DirCol(1,1),
			DirCol(-1,0),DirCol(0,0),DirCol(1,0),
			DirCol(-1,-1),DirCol(0,-1),DirCol(1,-1),
		};
		
		var k = rt.Length -1;
		
		for(int i=0; i<rt.Length; i++) rt[k-i] = c[flowDir[i]];
		return rt;
	}
	
	private Color DirCol(float x, float y) {
		var v = new Vector2(x,y);
		v = v.normalized;
		v = v*0.5f;
		
		return new Color(v.x + 0.5f, v.y + 0.5f, 1,1);
		// return new Color(x*0.5f + 0.5f, y*0.5f + 0.5f, 1,1);
	}
	
}
