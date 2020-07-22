using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class FluidDevelopment : MonoBehaviour {
	
	public struct FluidData {
		public Vector3 position;
		public Vector2 velocity;
		public float density, curl, divergence;
	}
	
	public int width = 100, length = 100;
	public float cellsize = 0.1f;
	public Material renderMat;
	public ComputeShader fluidDevelopment;
	
	// private FluidData[] 
	
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
	
	public void OnRenderObject() {
		
	}
	
	void OnDrawGizmos() {
		Gizmos.DrawWireCube(Vector3.zero, new Vector3(width*cellsize,0,length*cellsize));
	}
	
}
