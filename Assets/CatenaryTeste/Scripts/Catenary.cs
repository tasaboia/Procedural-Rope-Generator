using UnityEngine;
using System.Collections;

public class Catenary : MonoBehaviour
{
	[Range(0.01f,1f)]
	public float wireRadius = 0.02f;
	[Range(1.5f,100f)]
	public float wireCatenary = 10f;
	[Range(0.1f,10f)]
	public float wireResolution = 0.1f;
	public Transform p1;
	public Transform p2;

	public PrimitiveType primitive = PrimitiveType.Cube;

	void Start ()
	{
		Regenerate ();
	}

	// Calcula o Cosseno hiperbólic
	float CosH(float t)
	{
		return (Mathf.Exp(t) + Mathf.Exp(-t))/2;
	}
	// Calcula a Catenaria
	public float calculateCatenary(float a, float x )
	{
		return a * CosH( x/a );
	}

	[ContextMenu ("Regenerate")] //para chamar no modo editor
	public void Regenerate() // encontra os pontos da catenaria
	{
		float distance = Vector3.Distance (p1.position, p2.position);
		int nPoints = (int) (distance/wireResolution + 1);

		wireResolution = distance / (nPoints-1);

		Vector3[] wirePoints = new Vector3[nPoints];
		wirePoints[0] = p1.position;
		wirePoints[nPoints-1] = p2.position;
		
		Vector3 dir = (p2.position - p1.position).normalized;
		float offset = calculateCatenary( wireCatenary, -distance/2 );
		
		for (int i = 1; i < nPoints - 1; ++i) {
			Vector3 wirePoint = p1.position + i * wireResolution * dir;

			float x = i * wireResolution - distance / 2;
			wirePoint.y = wirePoint.y - (offset - calculateCatenary (wireCatenary, x));
			
			wirePoints[i] = wirePoint;
		}
		GenerateWithPrimitive (wirePoints);
	}

	private void GenerateWithLine( Vector3[] wirePoints ) // using lineRender to illustrate the catenary //usa o line renderer para ilustrar a catenaria
	{
		LineRenderer line = GetComponent< LineRenderer > ();
		
		line.SetVertexCount (wirePoints.Length);

		for (int i = 0; i < wirePoints.Length; ++i) {
			line.SetPosition (i, wirePoints[i]);
		}
	}

	private void GenerateWithPrimitive( Vector3[] wirePoints )
	{
		if (primitive == PrimitiveType.Plane || primitive == PrimitiveType.Quad)
			primitive = PrimitiveType.Cube;

		GameObject c = GameObject.CreatePrimitive (primitive); // type of segments to illustrate the catenary // ilustrar a catenaria com o segmento escolhido, a primitiva escolhida

		Mesh primitiveMesh = c.GetComponent< MeshFilter >().sharedMesh;
		float primitiveHeight = GetHeightByPrimitive (primitive);


		MeshFilter meshFilter = GetComponent< MeshFilter > ();
//		if( meshFilter )
//			DestroyImmediate (meshFilter);
//
//		meshFilter = gameObject.AddComponent< MeshFilter > ();

		MeshCollider meshCollider = GetComponent< MeshCollider > ();
//		if( meshCollider )
//			DestroyImmediate (meshCollider);
//		
//		meshCollider = gameObject.AddComponent< MeshCollider > ();


		Mesh mesh = new Mesh ();
		mesh.name = "Douglas&Taina";

		int numSegments = wirePoints.Length - 1;

		//create the mesh. the mesh need a vertices, a triangles, a nomals and uv // criando o mesh. um mesh precisa de vertices, triangulos, normais e uv
		Vector3[] vertices = new Vector3[ numSegments * primitiveMesh.vertices.Length ];
		int[] triangles = new int[ numSegments * primitiveMesh.triangles.Length ];
		Vector3[] normals = new Vector3[ numSegments * primitiveMesh.normals.Length ];
		Vector2[] uv = new Vector2[ numSegments * primitiveMesh.uv.Length ];

		
		transform.position = (wirePoints [0] + wirePoints [numSegments]) / 2; // put the pivot at the centre of the mesh// colocar o pivot no centro do 
			
		Matrix4x4 worldTx = transform.localToWorldMatrix.inverse;

		for (int segment = 0; segment < numSegments; ++segment) 
		{
			Vector3 txT = wirePoints[segment]; // the vector3s of catenary. get to use to translate the mesh //pegando as posicoes dada pela catenaria para transladar o mesh

			Quaternion txR = Quaternion.FromToRotation (Vector3.up, wirePoints[segment+1] - wirePoints[segment]); // change the rotation of original mesh to the catenary point //mudando a rotaçao do mesh original para a rotacao em que a catenaria deve estar

			Vector3 txS = new Vector3 (2 * wireRadius, (wirePoints[segment+1] - wirePoints[segment]).magnitude / primitiveHeight, 2 * wireRadius); // change de scale according to the primitive type// mudando a escala de acordo com a primitiva escolhida.

			Matrix4x4 preTx = Matrix4x4.TRS(new Vector3(0.0f,primitiveHeight / 2.0f,0.0f), Quaternion.identity, Vector3.one); //levar em consideracao o primeiro ponto.

			Matrix4x4 tx = worldTx * Matrix4x4.TRS (txT, txR, txS) * preTx;

			int verticesOffset = segment * primitiveMesh.vertices.Length;

			for (int i = 0; i < primitiveMesh.vertices.Length; ++i) {
				vertices [verticesOffset + i] = tx.MultiplyPoint (primitiveMesh.vertices [i]);
			}

			int trianglesOffset = segment * primitiveMesh.triangles.Length;

			for (int i = 0; i < primitiveMesh.triangles.Length; ++i) {
				triangles [trianglesOffset + i] = primitiveMesh.triangles[i] + verticesOffset;
			}

			int normalsOffset = segment * primitiveMesh.normals.Length;
			
			for (int i = 0; i < primitiveMesh.normals.Length; ++i) {
				normals [normalsOffset + i] = txR * primitiveMesh.normals[i];
			}

			int uvOffset = segment * primitiveMesh.uv.Length;
			
			for (int i = 0; i < primitiveMesh.uv.Length; ++i) {
				uv [uvOffset + i] = primitiveMesh.uv[i];
			}
		}

		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.normals = normals;
		mesh.uv = uv;

		mesh.RecalculateBounds ();

		meshFilter.mesh = mesh;
		meshCollider.sharedMesh = mesh;

		mesh.UploadMeshData (true);

		DestroyImmediate (c);
	}

	private float GetHeightByPrimitive (PrimitiveType type)
	{
		switch (type) {
		case PrimitiveType.Cube:
			return 1.0f;
		case PrimitiveType.Capsule:
		case PrimitiveType.Cylinder:
			return 2.0f;
		default:
			return 1.0f;
		}
	}
}

