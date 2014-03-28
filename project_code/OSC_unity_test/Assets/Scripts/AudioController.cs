using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioController : MonoBehaviour {

	public GameObject camera;
	public AudioSource Player;
	public AudioClip Clip;
	string[] songs = new string[4];
	int qSamples = 64;
	private float[] samples;
	// Use this for initialization
	private bool waveRendered = false;
	private List<float[]> sampleArray;
	private float fSample;
	private int frame = 0;

	private Vector3[] vertArrayMember;
	private int[] triArrayMember;
	private BeatDetektorJS bd;

	float[] array;
	int count;
	int buffersize;
	int beatcounterlast;

	Main main;

	GameObject Wave;

	void Start () {
		camera = GameObject.Find("Main Camera");
		this.Player = camera.AddComponent<AudioSource>();
		songs[0] = "Audio/Cirrus";
		samples = new float[qSamples];
		fSample = AudioSettings.outputSampleRate;
		Clip = (AudioClip)Resources.Load(songs[0].ToString());			
		Player.clip = Clip;
		Player.Play();
		//Player.volume = 0.0f;
		SetupWave();
		vertArrayMember = new Vector3[(samples.Length * 3) + 2];
		triArrayMember = new int[(samples.Length * 3) * 4];
		//SimpleMesh();
		sampleArray = new List<float[]>();


		buffersize = 128;
		bd = new BeatDetektorJS();
		array = new float[buffersize];
		bd.init(85.0f, 169.0f);
		main = GameObject.Find("Main Camera").GetComponent<Main>();

		VisManager vismanager = GameObject.Find("Main Camera").GetComponent<VisManager>();
		vismanager.SetAudioSource(Player);

	}




	// Update is called once per frame
	void Update () {
		//Player.GetOutputData(samples,0);				
		//RenderWaveCircleOutside(samples);

		Player.GetSpectrumData(array, 0, FFTWindow.BlackmanHarris);
		                         

				
		bd.process(Time.time, array);
		
		if(bd.beat_counter != beatcounterlast)
		{
			//GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);     
			//GameObject cube = Instantiate(Resources.Load("vuBar")) as GameObject;
			//cube.AddComponent("Rigidbody");
			//cube.transform.position = new Vector3(-10-(x*3), 7, -7);
			//Debug.Log(bd.beat_counter);
			//this.x++;
			//this.y++;

			foreach(var ball in main.balls)
			{
				if (ball.brain.activeState.GetType() != typeof(BallStateAimless))
				{
					//ball.light.intensity = 2.0f;
				}
			}

		}
		beatcounterlast = bd.beat_counter;
		main.beatCount = bd.beat_counter;

		var lightModifier = GetRMS ();


	}

	float GetRMS() {
		float sum = 0f;
		for (int i=0; i < this.array.Length; i++){
			sum += this.array[i]*this.array[i]; // sum squared samples
		}
		return Mathf.Sqrt(sum/this.array.Length); // rms = square root of average
	}

	void SetupWave(){
		Wave = new GameObject("WaveForm");
		Wave.AddComponent<MeshFilter>();
		Wave.AddComponent<MeshRenderer>();
		Material blockMaterial = new Material(Shader.Find("Diffuse"));
		blockMaterial.SetColor(1, new Color(255.0f,0.5f,0.2f,0.3f));
		if(blockMaterial) Wave.renderer.material = blockMaterial;
		Wave.AddComponent<MeshCollider>();
	}

	void RenderWaveLine(float[] samples){
		var sameWave = GameObject.Find("WaveForm");
		var waveMesh = new Mesh();
		//Init vert array with 2 x triangles, plus one. Minimum mesh
		var vertArray = new Vector3[(samples.Length * 2) + 1];
		var triArray = new int[samples.Length*3];

		for (int i = 0; i < samples.Length; i++)
		{
			vertArray[2*i] 	   = new Vector3(2.0f * (float)i, 0.0f);
			vertArray[(2*i)+1] = new Vector3((2.0f * (float)i) + 0.5f, samples[i] * 100f);
			triArray[(i*3)] = i * 2;
			triArray[(i*3) + 1] = (i * 2) + 1;
			triArray[(i*3) + 2] = (i * 2) + 2;
		}
		//final position, 0 height
		vertArray[(samples.Length * 2)] = new Vector3(2.0f * (float)(samples.Length) + 1, 0.0f);

		waveMesh.vertices = vertArray;
		
		var uvs = new Vector2[waveMesh.vertices.Length]; 
		for (var i=0; i<uvs.Length; i++) {
			uvs[i] = new Vector2(waveMesh.vertices[i].x, waveMesh.vertices[i].z);
		}
		waveMesh.uv = uvs;

		waveMesh.triangles = triArray;
		waveMesh.RecalculateNormals();

		sameWave.GetComponent<MeshFilter>().mesh = waveMesh;
	}
	void RenderWaveCircle(float[] samples){
		var sameWave = GameObject.Find("WaveForm");
		var waveMesh = new Mesh();
		var vertArray = new Vector3[(samples.Length * 2) + 1];
		var triArray = new int[samples.Length *3];
		var distance = 50f;
		var angleIncrements = (360f / ((float)samples.Length)) * Mathf.Deg2Rad;
		
		//result.Y = (int)Math.Round( centerPoint.Y + distance * Math.Sin( angle ) );
		//result.X = (int)Math.Round( centerPoint.X + distance * Math.Cos( angle ) );
		
		for (int i = 0; i < samples.Length ; i++)
		{
			vertArray[2*i] 	   = new Vector3(
				distance * Mathf.Cos( angleIncrements * i ), 
				distance * Mathf.Sin( angleIncrements * i ));
			
			vertArray[(2*i)+1] = new Vector3(
				(distance + (samples[i] * 150f)) * Mathf.Cos( angleIncrements * i ), 
				(distance + (samples[i] * 150f)) * Mathf.Sin( angleIncrements * i ));
			
			//vertArray[(2*i)+1] = new Vector3(
			//	(2.0f * (float)i) + 1.0f, 
			//	samples[i] * 100f);
			
			triArray[(i*3)] = i * 2;
			triArray[(i*3) + 1] = (i * 2) + 1;
			triArray[(i*3) + 2] = (i * 2) + 2;
		}
		//print(triArray.Length);
		//final position, 0 height
		
		//vertArray[(samples.Length * 2)] = new Vector3((float)(samples.Length * 2) + 1f, 0.0f);
		waveMesh.vertices = vertArray;		
		var uvs = new Vector2[waveMesh.vertices.Length]; 
		for (var i=0; i<uvs.Length; i++) {
			uvs[i] = new Vector2(waveMesh.vertices[i].x, waveMesh.vertices[i].z);
		}
		waveMesh.uv = uvs;
		waveMesh.triangles = triArray;
		waveMesh.RecalculateNormals();
		sameWave.GetComponent<MeshFilter>().mesh = waveMesh;
	}
	void RenderWaveCircleOutside(float[] samples){
		var sameWave = GameObject.Find("WaveForm");
		var waveMesh = new Mesh();
		var vertArray = new Vector3[(samples.Length * 2) + 1];
		var triArray = new int[samples.Length *3];
		var distance = 50f;
		var angleIncrements = (180f / ((float)samples.Length)) * Mathf.Deg2Rad;
		
		//result.Y = (int)Math.Round( centerPoint.Y + distance * Math.Sin( angle ) );
		//result.X = (int)Math.Round( centerPoint.X + distance * Math.Cos( angle ) );
		
		for (int i = 0; i < samples.Length ; i++)
		{
			vertArray[2*i] 	   = new Vector3(
				distance * Mathf.Cos( angleIncrements * i ), 
				distance * Mathf.Sin( angleIncrements * i ));
			
			vertArray[(2*i)+1] = new Vector3(
				(distance + (samples[i] * 150f)) * Mathf.Cos( angleIncrements * i ), 
				(distance + (samples[i] * 150f)) * Mathf.Sin( angleIncrements * i ));
			
			//vertArray[(2*i)+1] = new Vector3(
			//	(2.0f * (float)i) + 1.0f, 
			//	samples[i] * 100f);
			
			triArray[(i*3)] = i * 2;
			triArray[(i*3) + 1] = (i * 2) + 1;
			triArray[(i*3) + 2] = (i * 2) + 2;
		}
		//print(triArray.Length);
		//final position, 0 height
		
		//vertArray[(samples.Length * 2)] = new Vector3((float)(samples.Length * 2) + 1f, 0.0f);
		waveMesh.vertices = vertArray;		
		var uvs = new Vector2[waveMesh.vertices.Length]; 
		for (var i=0; i<uvs.Length; i++) {
			uvs[i] = new Vector2(waveMesh.vertices[i].x, waveMesh.vertices[i].z);
		}
		waveMesh.uv = uvs;
		waveMesh.triangles = triArray;
		waveMesh.RecalculateNormals();
		sameWave.GetComponent<MeshFilter>().mesh = waveMesh;
	}

	void RenderWaveLineDistanceSingle(float[] samples){

		var sameWave = GameObject.Find("WaveForm");
		var waveMesh = new Mesh();
		//waveMesh.Clear();
		//Init vert array with 2 x triangles, plus one. Minimum mesh
		
		var vertArray = new Vector3[5];
		var triArray = new int[12];
		float z = 0.0f;

		vertArray[0] = new Vector3(0.0f, 0.0f, 0.0f); //bottom left
		vertArray[1] = new Vector3(0.0f, 0.0f, 2.0f); //top left
		vertArray[2] = new Vector3(1f, Mathf.Abs(samples[128]) * 10f, 1f);
		vertArray[3] = new Vector3(2.0f, 0.0f, 0.0f); //bottom right
		vertArray[4] = new Vector3(2.0f, 0.0f, 2.0f); //top right		


		triArray[0] = 0;  //0	3	6
		triArray[1] = 1;  //1	4	7
		triArray[2] = 2;  //2	5	8
		
		triArray[3] = 1;  //1	4	7
		triArray[4] = 2;  //2	5	8
		triArray[5] = 4;  //4	7	10
		
		triArray[6] = 0;      //0	3	6
		triArray[7] = 2;  //2	5	8
		triArray[8] = 3;  //3	6	9
		
		triArray[9] = 2;   //2	5	8
		triArray[10] = 3;  //3	6	9
		triArray[11] = 4;  //4	7	10


		waveMesh.vertices = vertArray;
		
		var uvs = new Vector2[waveMesh.vertices.Length]; 
		for (var i=0; i<uvs.Length; i++) {
			uvs[i] = new Vector2(waveMesh.vertices[i].x, waveMesh.vertices[i].z);
		}
		waveMesh.uv = uvs;
		
		waveMesh.triangles = triArray;
		waveMesh.RecalculateNormals();
		
		sameWave.GetComponent<MeshFilter>().mesh = waveMesh;

	}

	void RenderWaveLineDistance(float[] samples){
		var waveMesh = GameObject.Find("WaveForm").GetComponent<MeshFilter>().mesh;
		waveMesh.Clear();
		//Init vert array with 2 x triangles, plus one. Minimum mesh



		float z = 0.0f;

		for (int i = 0; i < samples.Length; i++)
		{
			vertArrayMember[3*i] = new Vector3(2.0f * (float)i, 0.0f, z); //bottom left
			vertArrayMember[(3*i)+1] = new Vector3((2.0f * (float)i), 0.0f, z + 1.0f); //top left
			//vertArray[(2*i)+2] = new Vector3((2.0f * (float)i) + 1.0f, 0.0f, z); //bottom right
			//vertArray[(2*i)+3] = new Vector3((2.0f * (float)i) + 1.0f, 0.0f, z + 1.0f); //top right

			vertArrayMember[(3*i)+2] = new Vector3((2.0f * (float)i) + 0.5f, Mathf.Abs(samples[i]) * 100f, z + 0.5f);


			triArrayMember[(i*12)] = i * 3;            //0	3	6
			triArrayMember[(i*12) + 1] = (i * 3) + 1;  //1	4	7
			triArrayMember[(i*12) + 2] = (i * 3) + 2;  //2	5	8

			triArrayMember[(i*12) + 3] = (i * 3) + 1;  //1	4	7
			triArrayMember[(i*12) + 4] = (i * 3) + 2;  //2	5	8
			triArrayMember[(i*12) + 5] = (i * 3) + 4;  //4	7	10

			triArrayMember[(i*12) + 6] = (i * 3);      //0	3	6
			triArrayMember[(i*12) + 7] = (i * 3) + 2;  //2	5	8
			triArrayMember[(i*12) + 8] = (i * 3) + 3;  //3	6	9

			triArrayMember[(i*12) + 9] = (i * 3) + 2;   //2	5	8
			triArrayMember[(i*12) + 10] = (i * 3) + 3;  //3	6	9
			triArrayMember[(i*12) + 11] = (i * 3) + 4;  //4	7	10


			//for (int j = 0;j < 4; j ++)
			//{
			//	triArray[(i*12) + (j * 3)] = i * 2; 
			//	triArray[(i*12) + (j * 3) + 1] = (i * 2) + 1;
			//	triArray[(i*12) + (j * 3) + 2] = (i * 2) + 2;
			//}
		}
		//final position, 0 height

		vertArrayMember[(samples.Length * 3)] = new Vector3(2.0f * (float)(samples.Length), 0.0f, z);
		vertArrayMember[(samples.Length * 3) + 1] = new Vector3(2.0f * (float)(samples.Length), 0.0f, z + 1.0f);

		waveMesh.vertices = vertArrayMember;
		
		var uvs = new Vector2[waveMesh.vertices.Length]; 
		for (var i=0; i<uvs.Length; i++) {
			uvs[i] = new Vector2(waveMesh.vertices[i].x, waveMesh.vertices[i].z);
		}
		waveMesh.uv = uvs;
		
		waveMesh.triangles = triArrayMember;
		waveMesh.RecalculateNormals();
		
		//waveMesh.mesh = waveMesh;
	}

	void SimpleMesh() {
		Vector3 p1 = new Vector3(0.0f,0.0f,0.0f);
		Vector3 p2 = new Vector3(40.0f,40.0f,40.0f);
		float blockHeight = 1.0f;
		Material blockMaterial = new Material(Shader.Find("Diffuse"));
		blockMaterial.SetColor(1, new Color(255.0f,0.5f,0.2f,0.3f));
		//blockMaterial.color = new Vector4(0.5f,0.5f,0.2f,1.0f);
		PhysicMaterial blockPhysicMaterial;
		
		GameObject newLedge = new GameObject("testing");
		var newMesh = new Mesh();
		newLedge.AddComponent<MeshFilter>();
		newLedge.AddComponent<MeshRenderer>();
		
		
		
		Vector3 topLeftFront = new Vector3(p1.x,p1.y,p1.z);
		Vector3 topRightFront = new Vector3(p2.x,p2.y,p2.z);
		Vector3 topLeftBack = new Vector3(p1.x,p1.y,p1.z);
		Vector3 topRightBack = new Vector3(p2.x,p2.y,p2.z);
		Vector3 bottomLeftFront = new Vector3();
		Vector3 bottomRightFront = new Vector3();
		
		topRightFront.z = 0.5f;
		topLeftFront.z = 0.5f;
		topLeftBack.z = -0.5f;
		topRightBack.z = -0.5f;
		
		bottomLeftFront = topLeftFront;
		bottomRightFront = topRightFront;
		bottomLeftFront.y -= blockHeight; //remember the block height variable we defined?
		bottomRightFront.y -= blockHeight;
		
		newMesh.vertices = new Vector3[]{topLeftFront, topRightFront, topLeftBack, topRightBack, bottomLeftFront, bottomRightFront};
		
		var uvs = new Vector2[newMesh.vertices.Length]; 
		for (var i=0; i<uvs.Length; i++) {
			uvs[i] = new Vector2(newMesh.vertices[i].x, newMesh.vertices[i].z);
		}
		newMesh.uv = uvs;
		
		newMesh.triangles = new int[]{5, 4, 0, 0, 1, 5, 0, 2, 3, 3, 1, 0};
		newMesh.RecalculateNormals();
		//newLedge.GetComponent<MeshFilter>.mesh = newMesh;
		newLedge.GetComponent<MeshFilter>().mesh = newMesh;
		if(blockMaterial) newLedge.renderer.material = blockMaterial;
		
		
		
		newLedge.AddComponent<MeshCollider>();
		//if(blockPhysicMaterial) newLedge.GetComponent<MeshCollider>().material = blockPhysicMaterial;
		
		/*
		float size = 100.0f;
		Mesh m = new Mesh();
		m.name = "Scripted_Plane_New_Mesh";
		m.vertices = new Vector3[]{ new Vector3(-size, -size, 0.01f), new Vector3(size, -size, 0.01f), new Vector3(size, size, 0.01f), new Vector3(-size, size, 0.01f) };
		m.uv = new Vector2[]{new Vector2 (0f, 0f), new Vector2 (0f, 1f), new Vector2(1f, 1f), new Vector2 (1f, 0f)};
		m.triangles = new int[]{0, 1, 2, 0, 2};
		m.RecalculateNormals();
		var obj = new GameObject("New_Plane_Fom_Script");
		obj.AddComponent<MeshRenderer>();
		obj.AddComponent<MeshFilter>();
		obj.AddComponent<MeshCollider>();
		obj.GetComponent<MeshFilter>().mesh = m;
		*/

	}

}
