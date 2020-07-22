using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fluid_2D : MonoBehaviour {

	public int size = 512;
	[Range(8,32)]
	public int threadCount = 32;
	
	[Range(0.1f, 10)]
	public float minSpeed = 1f;
	
	public ComputeShader computeShader;
	
	
	public Material mat, flowmap;
	public string matName = "_MainTex", flowmapName = "_MainTex";
	
	public int iteration = 30;
	public float radio = 25f;
	public float consistence = 0.1f;
	public float damp = 0.98f;
	public float timeScale = 1f;
	public float mouseSpeedModify = 1;
	public float clearDamp = 0.8f;
	public float colorFadeOut = 1;
	
	public Color brushColor = new Color(1,1,1,1);
	
	protected ComputeBuffer[] velocity, density, color; 
	protected ComputeBuffer curled, divergenced;
	
	protected int vIndex, dIndex, cIndex;
	
	protected RenderTexture tex;
	protected RenderTexture flowmapTex;
	
	protected Vector3 mousePoint, mouseDelta;
	
	protected int threadNum;
	
	protected int 
		clean,
	
		advection,
		splat,
		curl,
		vorticity,
		divergence,
		clear,
		pressure,
		gradien,
		renderTex;
	
	protected virtual ComputeBuffer Read(ComputeBuffer[] bf, int index) {
		return bf[index];
	}
	
	protected virtual ComputeBuffer Write(ComputeBuffer[] bf, int index) {
		return bf[(index+1)%2];
	}
	
	protected virtual void Switch(ref int index) {
		index = (index+1)%2;
	}
	
	protected virtual ComputeBuffer[] GetDoubleBuffer(int length, int floatSize) {
		var fs = sizeof(float) * floatSize;
		return new ComputeBuffer[]{
			new ComputeBuffer(length, fs),
			new ComputeBuffer(length, fs),
		};
	}
	
	protected virtual ComputeBuffer GetBuffer(int length, int floatSize) {
		return new ComputeBuffer(length, sizeof(float) * floatSize);
	}
	
	// Use this for initialization
	protected virtual void Start () {
		int totalSize = size * size;
	
		velocity = GetDoubleBuffer(totalSize, 2);
		density = GetDoubleBuffer(totalSize, 1);
		color = GetDoubleBuffer(totalSize, 4);
		
		curled = GetBuffer(totalSize,1);
		divergenced = GetBuffer(totalSize,1);
		
		advection = computeShader.FindKernel("Advection");
		splat = computeShader.FindKernel("Splat");
		curl = computeShader.FindKernel("Curl");
		vorticity = computeShader.FindKernel("Vorticity");
		divergence = computeShader.FindKernel("Divergence");
		clear = computeShader.FindKernel("Clear");
		pressure = computeShader.FindKernel("Pressure");
		gradien = computeShader.FindKernel("GradienSubtract");
		renderTex = computeShader.FindKernel("RenderTex");
		
		clean = computeShader.FindKernel("Clean");
		
		tex = new RenderTexture(size,size,0,RenderTextureFormat.ARGB32);
		tex.enableRandomWrite = true;
		tex.Create();
		
		flowmapTex = new RenderTexture(size,size,0,RenderTextureFormat.ARGB32);
		flowmapTex.enableRandomWrite = true;
		flowmapTex.Create();
		
		computeShader.SetFloat("size", size);
		computeShader.SetFloat("minSpeed", minSpeed);
		
		computeShader.SetTexture(renderTex, "Result", tex);
		computeShader.SetTexture(renderTex, "Flowmap", flowmapTex);
		mat.SetTexture(matName, tex);
		flowmap.SetTexture(flowmapName, flowmapTex);
	}
	
	protected virtual void UpdateMouse() {
		var plane = new Plane(Vector3.up, Vector3.zero);
		var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		float dis = 0;
		
		plane.Raycast(ray, out dis);
		var point = ray.GetPoint(dis);
		
		mouseDelta = point - mousePoint;
		mousePoint = point;
	}
	
	protected virtual void DrawWithBrush() {
		SetMouseMovement();
		DispatchDrawing();
	}
	
	protected virtual void SetMouseMovement() {
		var r = radio / size;
		computeShader.SetFloat("radio", radio);
		computeShader.SetFloat("radio_ms", mouseDelta.magnitude * r);
		computeShader.SetFloats("mousePoint", new float[]{mousePoint.x/10f, mousePoint.z/10f});
		computeShader.SetFloats("mouseDelta", new float[]{mouseDelta.x * 10f * mouseSpeedModify, mouseDelta.z * 10f * mouseSpeedModify});	
	}
	
	protected virtual void DispatchDrawing() {
		var c = new float[]{
			brushColor.r * consistence,
			brushColor.g * consistence,
			brushColor.b * consistence,
			1,
		};
		computeShader.SetFloats("color", c);
		
		PushVelocityRW(splat);
		PushColorRW(splat);
		
		Switch(ref vIndex);
		Switch(ref cIndex);
		
		Dispatch(splat);
	}
	
	protected virtual bool InArea() {
		return mousePoint.x > 0 && mousePoint.x < 10 && mousePoint.z > 0 && mousePoint.z < 10;
	}
	
	// Update is called once per frame
	protected virtual void Update () {
		threadNum = size/threadCount;
		
		computeShader.SetFloat("deltaTime", Time.deltaTime * timeScale);
		computeShader.SetFloat("damp", damp);
		computeShader.SetFloat("clearDamp", clearDamp);
		computeShader.SetFloat("colorFadeOut", colorFadeOut);
		
		PushVelocityRW(advection);
		PushColorRW(advection);
		Switch(ref vIndex);
		Switch(ref cIndex);
		Dispatch(advection);
		
		UpdateMouse();
		if(Input.GetMouseButton(0) && InArea())
			DrawWithBrush();
		
		computeShader.SetBuffer(curl,"VelocityR", Read(velocity, vIndex));
		computeShader.SetBuffer(curl,"Curled", curled);
		Dispatch(curl);
		
		PushVelocityRW(vorticity);
		computeShader.SetBuffer(vorticity,"Curled", curled);
		Switch(ref vIndex);
		Dispatch(vorticity);
		
		computeShader.SetBuffer(divergence,"VelocityR", Read(velocity, vIndex));
		computeShader.SetBuffer(divergence,"Divergenced", divergenced);
		Dispatch(divergence);
		
		computeShader.SetBuffer(clear,"DensityW", Write(density, dIndex));
		Dispatch(clear);
		Switch(ref dIndex);
		
		computeShader.SetBuffer(pressure,"Divergenced", divergenced);
		for(int i=0; i<iteration; i++) {
			PushPressureRW(pressure);
			Dispatch(pressure);
			Switch(ref dIndex);
		}
		
		computeShader.SetBuffer(gradien,"DensityR", Read(density, dIndex));
		PushVelocityRW(gradien);
		Dispatch(gradien);
		
		computeShader.SetBuffer(renderTex,"DensityR", Read(density, dIndex));
		computeShader.SetBuffer(renderTex,"VelocityR", Read(velocity, vIndex));
		computeShader.SetBuffer(renderTex,"ColorR", Read(color, cIndex));
		Dispatch(renderTex);
		
		if(Input.GetKeyDown(KeyCode.B)) {
			PushVelocityRW(clean);
			PushColorRW(clean);
			PushPressureRW(clean);
			computeShader.SetBuffer(clean,"DensityW", Write(density, dIndex));
			computeShader.SetBuffer(clean,"Curled", curled);
			computeShader.SetTexture(clean, "Result", tex);
			Dispatch(clean);
			
			Switch(ref vIndex);
			Switch(ref cIndex);
			Switch(ref dIndex);
			
			PushVelocityRW(clean);
			PushColorRW(clean);
			PushPressureRW(clean);
			Dispatch(clean);
		}
	}
	
	protected virtual void Dispatch(int kernel) {
		computeShader.Dispatch(kernel, threadNum, threadNum, 1);
	}
	
	protected virtual void PushVelocityRW(int kernel) {
		computeShader.SetBuffer(kernel,"VelocityR", Read(velocity, vIndex));
		computeShader.SetBuffer(kernel,"VelocityW", Write(velocity, vIndex));
	}
	
	protected virtual void PushColorRW(int kernel) {
		computeShader.SetBuffer(kernel,"ColorR", Read(color, cIndex));
		computeShader.SetBuffer(kernel,"ColorW", Write(color, cIndex));
	}
	
	protected virtual void PushPressureRW(int kernel) {
		computeShader.SetBuffer(kernel,"DensityR", Read(density, dIndex));
		computeShader.SetBuffer(kernel,"DensityW", Write(density, dIndex));
	}
	
	protected virtual void OnGUI() {
		GUILayout.Label("CLEAN: B");
		GUILayout.Label("radio");
		radio = GUILayout.HorizontalSlider(radio,0.1f,50f,GUILayout.Width(200));
		GUILayout.Label("consistence");
		consistence  = GUILayout.HorizontalSlider(consistence,0f,0.99f,GUILayout.Width(200));
		GUILayout.Label("timeScale");
		timeScale  = GUILayout.HorizontalSlider(timeScale,0.5f,5f,GUILayout.Width(200));
		GUILayout.Label("iteration");
		iteration = (int)GUILayout.HorizontalSlider(iteration,10,100f,GUILayout.Width(200));
		GUILayout.Label("damp");
		damp  = GUILayout.HorizontalSlider(damp,0.01f,0.999f,GUILayout.Width(200));
		GUILayout.Label("clearDamp");
		clearDamp  = GUILayout.HorizontalSlider(clearDamp,0.01f,0.999f,GUILayout.Width(200));
		GUILayout.Label("colorFadeOut");
		colorFadeOut  = GUILayout.HorizontalSlider(colorFadeOut,0.95f,1f,GUILayout.Width(200));
		
		GUILayout.Label("mouseSpeedModify");
		mouseSpeedModify  = GUILayout.HorizontalSlider(mouseSpeedModify,0.1f,10f,GUILayout.Width(200));
	}
	
	protected virtual void OnDestroy() {
		velocity[0].Release();	
		velocity[1].Release();
		density[0].Release();	
		density[1].Release();
		color[0].Release();	
		color[1].Release();
		
		curled.Release();
		divergenced.Release();
		
		tex.Release();
	}
}
