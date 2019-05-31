using UnityEngine;
using System.Collections;

	namespace DualGraph2d{
	public class Circumcircle {
		public Cell[] verts{get; private set;}

		private Vector3 circumcenter;
		public Vector3 Circumcenter{
			get{
				return circumcenter;
			} 
			private set{
				circumcenter=value;
				GhostCheck();
			}
		}
		public float circumradius{get; private set;}
		public float circumradiusSquared{get; private set;}
		public float sortPoint{get; private set;}
		public bool ghost{get; private set;}
		
		private TriangulationEdge[] edges;
		public TriangulationEdge[] Edges{
			get{
				if (edges==null){
					DefineEdges();
				}
				return edges;
			}
		}
		
		public Circumcircle(Cell a, Cell b, Cell c){
			verts= new Cell[]{a,b,c};
			Vector3[] cellVerts= new Vector3[]{a.point, b.point, c.point};
			
			Circumcenter= GetCircumcenter(cellVerts);
			Init();
		}
		private void Init(){
			circumradiusSquared=Vector3.SqrMagnitude(circumcenter-verts[0].point);
			circumradius=Mathf.Sqrt(circumradiusSquared);
			AssignSelf();
			sortPoint= circumcenter.x+circumradius;
		}

		//manages sphere presence in cells
		public void AssignSelf(){
			foreach(Cell c in verts){
				c.AddSphere(this);
			}
		}
		public void RemoveSelf(){
			foreach(Cell c in verts){
				c.RemoveSphere(this);
			}
		}
		
		//Only defines triangles
		public void DefineEdges(){
			edges= new TriangulationEdge[3];
			edges[0]= new TriangulationEdge(verts[0],verts[1]);
			edges[1]= new TriangulationEdge(verts[1],verts[2]);
			edges[2]= new TriangulationEdge(verts[2],verts[0]);
		}

		/// <summary>
		/// Checks whether this circle is outside the area bounds.
		/// </summary>
		private void GhostCheck(){
			if(circumcenter.x>DualGraph.volume.x || circumcenter.x<-DualGraph.volume.x){
				ghost=true;
			}
			else if(circumcenter.z>DualGraph.volume.z || circumcenter.z<-DualGraph.volume.z){
				ghost=true;
			}
		}

		//TODO: make vertices dimensionally independent
		// This function taken in large part from http://scrawkblog.com/2014/06/16/delaunay-triangulation-in-unity/
		// Thank you scrawk for saving me hours of staring at the mathworld page going FFFFUUUUUUUUUU...
		private Vector3 GetCircumcenter (Vector3[] Vertices)
		{
			// From MathWorld: http://mathworld.wolfram.com/Circumcircle.html
			
			var points = Vertices;
			
			double[,] m = new double[3, 3];
			
			// x, y, 1
			for (int i = 0; i < 3; i++) {
				m[i, 0] = points[i].x;
				m[i, 1] = points[i].z;
				m[i, 2] = 1;
			}
			var a = Determinant(m);
			
			// size, y, 1
			for (int i = 0; i < 3; i++) {
				m[i, 0] = points[i].x*points[i].x + points[i].z*points[i].z;
			}
			var dx = -Determinant(m);
			
			// size, x, 1
			for (int i = 0; i < 3; i++) {
				m[i, 1] = points[i].x;
			}
			var dy = Determinant(m);
			
			// size, x, y
			//for (int i = 0; i < 3; i++) {
			//	m[i, 2] = points[i].y;
			//}
			//var c = -Det(m);
			
			var s = -1.0 / (2.0 * a);
			//var r = System.Math.Abs(s) * System.Math.Sqrt(dx * dx + dy * dy - 4 * a * c);
			
			return new Vector3((float)(s * dx), 0.0f, (float)(s * dy));
		}
		private double Determinant(double[,] m)
		{
			double fCofactor00 = m[1,1] * m[2,2] - m[1,2] * m[2,1];
			double fCofactor10 = m[1,2] * m[2,0] - m[1,0] * m[2,2];
			double fCofactor20 = m[1,0] * m[2,1] - m[1,1] * m[2,0];
			
			double fDet = m[0,0] * fCofactor00 + m[0,1] * fCofactor10 + m[0,2] * fCofactor20;
			
			return fDet;
		}
		
	}
}