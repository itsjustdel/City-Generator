using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace DualGraph2d{
	public class DualGraph {


		public List<Cell> cells;
		public List<Circumcircle> spheres;
		public static Vector3 volume{get; private set;}

		private bool seeded=false;
		private float rootMulti;

		public DualGraph(Vector3 bounds){
			cells= new List<Cell>();
			spheres= new List<Circumcircle>();

			volume= bounds;

		}

		/// <summary>
		/// Loads cells for dual graph.
		/// </summary>
		/// <param name="c">C.</param>
		/// <param name="multiplier">Root Multiplier.</param>
		public void DefineCells(Vector3[] c, float multiplier){
			if (!seeded){
				seed (multiplier);
			}
			foreach(Vector3 p in c){
				cells.Add(new Cell(p));
			}
		}

		/// <summary>
		/// Seeds cell list.
		/// </summary>
		/// <param name="multiplier">Multiplier.</param>
		private void seed(float multiplier){
			//seeding supertriangle points on xz plane
			Cell root= new Cell(new Vector3(0.0f, 0.0f, volume.z*3.0f*multiplier), true);
			cells.Add(root);
			
			root= new Cell(new Vector3(volume.x *3.0f*multiplier, 0.0f, volume.z *-1.5f*multiplier), true);
			cells.Add(root);
			
			root= new Cell(new Vector3(-volume.x *3.0f*multiplier, 0.0f, volume.z *-1.5f*multiplier), true);
			cells.Add(root);
			
			seeded=true;
		}

		/// <summary>
		/// Computes for all loaded cells.
		/// </summary>
		public void ComputeForAllCells(){
			Queue<Cell> cellQueue= new Queue<Cell>(cells);
			List<Circumcircle> rejects= new List<Circumcircle>();
			List<TriangulationEdge> edges= new List<TriangulationEdge>();
			List<int> edgeRejects= new List<int>();
			
			spheres.Add(new Circumcircle(cellQueue.Dequeue(), cellQueue.Dequeue(), cellQueue.Dequeue()));

			Cell currentCell=cellQueue.Dequeue();
			int listIndex;
			do{
				//checking sphere lists
				listIndex=0;
				do{
					if (SphereCollision(currentCell, spheres[listIndex])){
						spheres[listIndex].RemoveSelf();
						rejects.Add(spheres[listIndex]);
						spheres.RemoveAt(listIndex);
					}
					else{
						listIndex++;
					}
				}while(listIndex<spheres.Count);

				//bounds check
				if (rejects.Count>0){
					if (rejects.Count==1){												//not totally necessary but skips the edge comparisons
						foreach (TriangulationEdge e in rejects[0].Edges){
							spheres.Add(new Circumcircle(e.a, e.b, currentCell));
						}
					}
					else{
						//edges to list
						foreach(Circumcircle c in rejects){
							foreach(TriangulationEdge e in c.Edges){
								edges.Add(e);
							}
						}

						//edge duplicates to list
						for(int i=0; i<edges.Count-1; i++){
							for (int j=i+1; j<edges.Count;j++){
								if(!edgeRejects.Contains(j)){
									if(edges[i].Is(edges[j])){
										//appears to only impact performance but does no evil?
										//if (!edgeRejects.Contains(i)){
											edgeRejects.Add(i);
										//}
										//if (!edgeRejects.Contains(j)){
											edgeRejects.Add(j);
										//}
									}
								}
							}
						}
						//removing dup edges from edge list
						edgeRejects.Sort();
						for(int i= edgeRejects.Count-1; i>=0; i--){
							edges.RemoveAt(edgeRejects[i]);
						}
						//edges to new spheres
						foreach(TriangulationEdge e in edges){
							spheres.Add(new Circumcircle(e.a, e.b, currentCell));
						}

					}
					// pops and clears for next pass
					if (cellQueue.Count>0){
						currentCell=cellQueue.Dequeue();
					}
					else
						currentCell=null;

					edges.Clear();
					edgeRejects.Clear();
					rejects.Clear();
				}
				else{
					Debug.LogError("no collisions: "+currentCell.point);
					currentCell=cellQueue.Dequeue();
				}

			}while(currentCell!=null);


		}

		/// <summary>
		/// Computes for all loaded cells.
		/// Expects all cells after the seeds to be sorted by x value
		/// </summary>
		public void ComputeForAllSortedCells(){
			Queue<Cell> cellQueue= new Queue<Cell>(cells);
			//list used to keep of spheres to check against
			List<Circumcircle> spheresAhead= new List<Circumcircle>();
			List<Circumcircle> rejects= new List<Circumcircle>();
			List<TriangulationEdge> edges= new List<TriangulationEdge>();
			List<int> edgeRejects= new List<int>();
			float x;

			spheresAhead.Add(new Circumcircle(cellQueue.Dequeue(), cellQueue.Dequeue(), cellQueue.Dequeue()));
			
			Cell currentCell=cellQueue.Dequeue();
			int listIndex;
			do{
				//checking sphere lists
				listIndex=0;
				x=currentCell.point.x;
				do{
					if(spheresAhead[listIndex].sortPoint<x){
						spheres.Add(spheresAhead[listIndex]);
						spheresAhead.RemoveAt(listIndex);
					}
					else if (SphereCollision(currentCell, spheresAhead[listIndex])){
						spheresAhead[listIndex].RemoveSelf();
						rejects.Add(spheresAhead[listIndex]);
						spheresAhead.RemoveAt(listIndex);
					}
					else{
						listIndex++;
					}
				}while(listIndex<spheresAhead.Count);
				
				//bounds check
				if (rejects.Count>0){
					if (rejects.Count==1){												//not totally necessary but skips the edge comparisons
						foreach (TriangulationEdge e in rejects[0].Edges){
							spheresAhead.Add(new Circumcircle(e.a, e.b, currentCell));
						}
					}
					else{
						//edges to list
						foreach(Circumcircle c in rejects){
							foreach(TriangulationEdge e in c.Edges){
								edges.Add(e);
							}
						}
						
						//edge duplicates to list
						for(int i=0; i<edges.Count-1; i++){
							for (int j=i+1; j<edges.Count;j++){
								if(!edgeRejects.Contains(j)){
									if(edges[i].Is(edges[j])){
										//appears to only impact performance but does no evil?
										//if (!edgeRejects.Contains(i)){
										edgeRejects.Add(i);
										//}
										//if (!edgeRejects.Contains(j)){
										edgeRejects.Add(j);
										//}
									}
								}
							}
						}
						//removing dup edges from edge list
						edgeRejects.Sort();
						for(int i= edgeRejects.Count-1; i>=0; i--){
							edges.RemoveAt(edgeRejects[i]);
						}
						//edges to new spheres
						foreach(TriangulationEdge e in edges){
							spheresAhead.Add(new Circumcircle(e.a, e.b, currentCell));
						}
						
					}
					// pops and clears for next pass
					if (cellQueue.Count>0){
						currentCell=cellQueue.Dequeue();
					}
					else
						currentCell=null;
					
					edges.Clear();
					edgeRejects.Clear();
					rejects.Clear();
				}
				else{
					Debug.LogError("no collisions: "+currentCell.point);
					currentCell=cellQueue.Dequeue();
				}
				
			}while(currentCell!=null);

			//dumping remainders into main list
			foreach(Circumcircle c in spheresAhead){
				spheres.Add(c);
			}
			
		}

		/// <summary>
		/// Checks for Sphere collision.
		/// </summary>
		/// <returns><c>true</c>, if cell is inside sphere, <c>false</c> otherwise.</returns>
		/// <param name="c">C.</param>
		/// <param name="s">S.</param>
		private bool SphereCollision(Cell c, Circumcircle s){
			Vector3 dif=c.point-s.Circumcenter;
			if (dif.sqrMagnitude <= s.circumradiusSquared){								//using square mag for xz plane
				return true;
			}
			else{
				return false;
			}
		}

		/// <summary>
		/// Prepares the cells for exporting to mesh.
		/// </summary>
		public void PrepareCellsForMesh(){
			foreach(Cell c in cells){
				if(!c.root){
					c.MeshSetup();
				}
			}
		}
	}

	/// <summary>
	/// TriangulationEdge.
	/// </summary>
	public class TriangulationEdge {
		
		public Cell a;
		public Cell b;
		
		public TriangulationEdge(Cell a2, Cell b2){
			a=a2;
			b=b2;
		}
		
		public bool Is(TriangulationEdge e){
			if(e.a== a){
				if (e.b==b){
					return true;
				}
			}
			else if (e.a==b){
				if (e.b==a){
					return true;
				}
			}
			return false;
		}
	}
}