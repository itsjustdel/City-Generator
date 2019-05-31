using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace DualGraph2d{
	public class Cell {
		public Vector3 point;
		public List<VoronoiEdge> edges;
		public bool root= false;
		/// <summary>
		/// Cell data compiled for mesh.
		/// </summary>
		public CellMesh mesh {get; private set;}

		private int ghostSpheres;


		public Cell(Vector3 p){
			point=p;
			edges= new List<VoronoiEdge>();
		}
		public Cell(Vector3 p, bool isRoot){
			point=p;
			root= isRoot;
			edges= new List<VoronoiEdge>();
		}


		public void AddSphere(Circumcircle s){
			if(s.ghost){
				ghostSpheres++;
			}

			foreach(Cell c in s.verts){
				if(c!=this){
					VoronoiEdge v= FindEdge(c);

					if (v!=null){
						v.SpherePair=s;
					}
					else{
						v= new VoronoiEdge(c,s);
						edges.Add(v);
					}
				}
			}
		}
		private VoronoiEdge FindEdge(Cell c){
			foreach(VoronoiEdge e in edges){
				if(e.cellPair== c)
					return e;
			}
			return null;
		}

		public void RemoveSphere(Circumcircle s){
			if(s.ghost){
				ghostSpheres--;
			}

			for(int i=edges.Count-1; i>=0; i--){
				if(edges[i].ContainsSphere(s) && edges[i].RemovingOnlySphere(s)){
					edges.RemoveAt(i);
				}
			}
		}

		/// <summary>
		/// Setup for mesh creation.
		/// Sorts edge list into a sequential chain clockwise and adds vertices into mesh.
		/// </summary>
		// TODO: look into using a clean list instead of reshuffling
		public void MeshSetup(){
			if(!root){
				PrepareFirstEdge();

				mesh= new CellMesh(point, edges.Count);

				Circumcircle current= edges[0].SpherePair;
				VoronoiEdge next;						//not really needed but it works while it's here

				mesh.AddVert(edges[0].Sphere.Circumcenter);

				int i=0,j=0;

				do{
					mesh.AddVert(current.Circumcenter);

					j=i;
					i++;

					//searching for next edge in the chain
					do{
						j++;
						if(j>=edges.Count){
							Debug.Log("no matching edge found");
						}
					}while(current!=edges[j].Sphere && current!=edges[j].SpherePair);

					// moving edge to the point it should be in the list if it isn't already there
					if(i!=j){
						next = edges[j];
						edges.RemoveAt(j);
						edges.Insert(i,next);
					}

					// checking for it's orientation
					if (current== edges[i].SpherePair){
						edges[i].Flip();
					}

					current= edges[i].SpherePair;

				}while(current!=edges[0].Sphere);
			
				mesh.ComputeUVs();
			}

		}

		// TODO: Currently should only work on flat graphs
		// any changes to dimensionality will probably require modification
		private void PrepareFirstEdge(){
			Vector3 norm= new Vector3();
			if (edges[0].isConnected){
				norm = Vector3.Cross(edges[0].Sphere.Circumcenter-point,edges[0].SpherePair.Circumcenter-point);
			}
			else{
				Debug.Log("not connected");
			}

			if (norm.y<0){
				edges[0].Flip();
			}
		}

		/// <summary>
		/// Purges extra/unconnected edges.
		/// Only used for root cells, but not needed.
		/// </summary>
		private void PurgeExtras(){
			int c=0	;
			for(int i=0; i<edges.Count;i++){
				if(!edges[i].isConnected){
					if(edges[i].Sphere==null&& edges[i].SpherePair==null){
						Debug.Log("edge not removed");
					}

					edges.RemoveAt(i);
					i--;
					c++;
				}
			}
			if(c>0){
				Debug.Log(c+" unconnected " + root);
			}
		}

		/// <summary>
		/// Determines whether this instance is open to the edge of the graph.
		/// </summary>
		/// <returns><c>true</c> if this instance is open edge; otherwise, <c>false</c>.</returns>
		public bool IsOpenEdge(){
			if(ghostSpheres>0)
				return true;
			else
				return false;
		}
	}

	/// <summary>
	/// Voronoi edge.
	/// Used to define edges of Voronoi graph.
	/// </summary>
	public class VoronoiEdge{
		public Cell cellPair;
		//primary
		private Circumcircle sphere;
		public Circumcircle Sphere{get{return sphere;}}				//mostly to keep naming conventions
		//secondary
		private Circumcircle spherePair;
		public Circumcircle SpherePair{
			get{return spherePair;}
			set{
				spherePair=value;
				DefineGhostLevel();
			}
		}
		/// <summary>
		/// Indicating whether this <see cref="DualGraph2d.VoronoiEdge"/> has a connected pair of spheres.
		/// </summary>
		/// <value><c>true</c> if is connected; otherwise, <c>false</c>.</value>
		public bool isConnected{
			get{
				if(sphere!=null && spherePair!=null)
					return true;
				else
					return false;
			}
		}
		public Ghosting ghostStatus{get; private set;}
		
		public VoronoiEdge(Cell c2, Circumcircle s1){
			cellPair= c2;
			sphere= s1;
		}

		/// <summary>
		/// Checks whether this edge contains the sphere.
		/// </summary>
		/// <returns><c>true</c>, if sphere was contained, <c>false</c> otherwise.</returns>
		/// <param name="s">S.</param>
		public bool ContainsSphere(Circumcircle s){
			if (sphere==s || spherePair==s)
				return true;
			else
				return false;
		}

		/// <summary>
		/// Removes sphere from edge.
		/// </summary>
		/// <returns><c>true</c>, if edge now contains no spheres, <c>false</c> otherwise.</returns>
		/// <param name="s">S.</param>
		public bool RemovingOnlySphere(Circumcircle s){
			if(sphere==s){
				if(spherePair==null){
					return true;
				}
				else{
					ReduceSpheres();
				}
			}
			else if(spherePair==s){
				if (sphere==null){
					return true;
				}
				else{
					spherePair=null;
				}
			}
			return false;
		}
		
		public void ReduceSpheres(){
			sphere=spherePair;
			spherePair=null;
		}

		/// <summary>
		/// Flips spheres.
		/// Used for orienting clockwise.
		/// </summary>
		public void Flip(){
			Circumcircle c= spherePair;
			spherePair=sphere;
			sphere=c;
		}

		private void DefineGhostLevel(){
			if(sphere.ghost){
				if(spherePair.ghost){
					ghostStatus= Ghosting.total;
				}
				else{
					ghostStatus= Ghosting.partial;
				}
			}
			else if(spherePair.ghost){
				ghostStatus= Ghosting.partial;
			}
			else{
				ghostStatus= Ghosting.none;
			}
		}
	}
	//Used by edges in determining whether edge is within bounds.
	public enum Ghosting{none,partial,total};

	/// <summary>
	/// Cell mesh.
	/// </summary>
	public class CellMesh{
		public Vector3[] verts {get; private set;}				//center is 0 index;
		public Vector2[] uv {get; private set;}
		public Vector3 minorCorner {get; private set;}
		public Vector3 majorCorner {get; private set;}
		public Vector3 area {get; private set;}					// will cause uneven scaling along dimensions, use float to keep square
		public float areaSqrd {get; private set;}				// biggest dimension of area, for square and more accurately tiled UV's

		//keeps track of vertice count
		private int step=0;

		public CellMesh(Vector3 center, int edgeCount){
			verts= new Vector3[edgeCount+1];
			uv= new Vector2[edgeCount+1];

			verts[step]=center; 
			step++;

			majorCorner+= center;
			minorCorner+= center;
		}

		public void AddVert(Vector3 v){
			majorCorner= Vector3.Max(majorCorner,v);
			minorCorner= Vector3.Min(minorCorner,v);

			verts[step]=v;
			step++;
		}

		public void ComputeUVs(){
			area= majorCorner-minorCorner;
			if (area.x>area.y && area.x>area.z){
				areaSqrd=area.x;
			}
			else if (area.y>area.z){
				// This is here more for completeness, but will probably not be the one you want
				areaSqrd=area.y;			
			}
			else{
				areaSqrd=area.z;
			}

			Vector3 temp= new Vector3();
			for(int i=0; i<verts.Length; i++){
				temp = verts[i]-minorCorner;
				uv[i]= new Vector2(temp.x/areaSqrd, temp.z/areaSqrd);
			}
		}
	}
}