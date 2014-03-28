using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ColorHolder{
	public Color color;
	public bool taken = false;
}

public class CornerHolder{
	public List<Vector3> points = new List<Vector3>();
	public bool taken = false;
}

public class Main : MonoBehaviour {

	// Use this for initialization


		


	public List<Ball> balls = new List<Ball>();
	public GameObject floor;
	public BeatDetektorJS bd;
	float[] array;
	int count;
	int buffersize;
	int beatcounterlast;
	AudioController audio;

	public int beatCount;
	private int lastBeatUsed;
	public List<ColorHolder> colors = new List<ColorHolder>();

	public List<CornerHolder> corners = new List<CornerHolder>();


	  void Awake() {
        //Application.runInBackground = true;
    }

	void Start () {


		var modX = -7.5f;
		var modY = -7.5f;

			corners.Add (new CornerHolder (){ 
			taken = false,
				points = new List<Vector3>() {
				new Vector3(0f+ modX,0,0f + modY),
				new Vector3(0f+ modX,0,10f+ modY),
				new Vector3(10f+ modX,0,10f+ modY),
				new Vector3(10f+ modX,0,0f+ modY)
			}});

		modX = -7.5f;
		modY = -2.5f;

		corners.Add (new CornerHolder (){ 
			taken = false,
			points = new List<Vector3>() {
				new Vector3(0f+ modX,0,0f + modY),
				new Vector3(0f+ modX,0,10f+ modY),
				new Vector3(10f+ modX,0,10f+ modY),
				new Vector3(10f+ modX,0,0f+ modY)
			}});

		modX = -2.5f;
		modY = -2.5f;

		corners.Add (new CornerHolder (){ 
			taken = false,
			points = new List<Vector3>() {
				new Vector3(0f+ modX,0,0f + modY),
				new Vector3(0f+ modX,0,10f+ modY),
				new Vector3(10f+ modX,0,10f+ modY),
				new Vector3(10f+ modX,0,0f+ modY)
			}});

		modX = -2.5f;
		modY = -7.5f;

		corners.Add (new CornerHolder (){ 
			taken = false,
			points = new List<Vector3>() {
				new Vector3(0f+ modX,0,0f + modY),
				new Vector3(0f+ modX,0,10f+ modY),
				new Vector3(10f+ modX,0,10f+ modY),
				new Vector3(10f+ modX,0,0f+ modY)
			}});

		colors.Add(new ColorHolder(){ color = new Color(115/255.0F,0,0)}); //red mid
		colors.Add(new ColorHolder(){ color = new Color(0,0,115.0f/255.0F)}); //red blue
		colors.Add(new ColorHolder(){ color = new Color(0,115.0f/255.0F,0)}); //red green
		colors.Add(new ColorHolder(){ color = new Color(242.0f/255.0F,89.0f/255.0F,5.0f/255.0F)}); //orange
		colors.Add(new ColorHolder(){ color = new Color(0,164.0f/255.0F,164.0f/255.0F)}); //blue green
		colors.Add(new ColorHolder(){ color = new Color(152.0f/255.0F,62.0f/255.0F,231.0f/255.0F)}); //purple

	for (int i = 0; i < 12; i++)
	{
		balls.Add(new Ball("ball" + i.ToString(), new Vector3(Random.Range(-10,10),5,Random.Range(-10,10))));
	}

		//var verts = floor.GetComponent<MeshFilter>().mesh.vertices;



	}
	
	// Update is called once per frame
	void Update () {
		//def proccess:
		//time_passed_seconds = time_passed / 1000.0
		//for entity in self.entities.itervalues():
		//	entity.process(time_passed_seconds)

		if (beatCount != lastBeatUsed)
		{
			if (Input.GetKey("1"))
			{
				balls[0].Trigger();
			}

			if (Input.GetKey("2"))
			{
				balls[1].Trigger();
			}

			if (Input.GetKey("3"))
			{
				balls[2].Trigger();
			}

			if (Input.GetKey("4"))
			{
				balls[3].Trigger();
			}
		}
		//Debug.Log ("current " + beatCount.ToString() + "lastUsed " + lastBeatUsed.ToString());

		lastBeatUsed = beatCount;

		foreach(var ball in balls)
		{
			ball.Process(Time.deltaTime);
		}
	}

	void DemoSplineDance(){
		var singleDancer = (GameObject)Instantiate(Resources.Load("Prefabs/Sphere"),new Vector3(0,5,0),new Quaternion());
		var baseSpline = new GameObject("baseSpline");
		//baseSpline.transform.position = new Vector3(0,0,0);
		var spline = baseSpline.AddComponent<CurvySpline>();
		var controlPoints = new Vector3[]{
			new Vector3(10,0,10), 
			new Vector3(0,0,10),
			new Vector3(0,0,0),
			new Vector3(10,0,0)
		};
		spline.Add(controlPoints);
		
		var walker = singleDancer.AddComponent<SplineWalker>();
		
		walker.Spline = spline;
		walker.Clamping = CurvyClamping.PingPong;
		walker.Speed = 0.5f;

		var curveyRender = this.GetComponent<GLCurvyRenderer>();
		curveyRender.Splines = new CurvySplineBase[]{spline};
		//CurvySpline spline = new CurvySpline();
		
		//spline.Add(new CurvySplineSegment(){Position=new Vector3(5,0,0)});
		//spline.Add(new CurvySplineSegment(){Position=new Vector3(10,4,0)});
		//spline.Add(new CurvySplineSegment(){Position=new Vector3(5,4,5)});
	}
}
