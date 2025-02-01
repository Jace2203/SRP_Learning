using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshBall : MonoBehaviour
{
    private const int NUM = 128;

    private static int baseColorId  = Shader.PropertyToID("_BaseColor");

    [SerializeField]
    private Mesh mesh = default;

    [SerializeField]
    private Material material = default;

    private Matrix4x4[] matrices = new Matrix4x4[NUM];
    private Vector4[] baseColors = new Vector4[NUM];

    private MaterialPropertyBlock block;

    public void Awake()
    {
        for (int i = 0; i < NUM; i++)
        {
            matrices[i] = Matrix4x4.TRS(
                Random.insideUnitCircle * 3f,
                Quaternion.identity,
                Vector3.one
            );
            baseColors[i] = new Vector4(Random.value, Random.value, Random.value, 1f);
        }
    }

    public void Update()
    {
        if (block == null)
        {
            block = new MaterialPropertyBlock();
            block.SetVectorArray(baseColorId, baseColors);
        }
        Graphics.DrawMeshInstanced(mesh, 0, material, matrices, NUM, block);
    }
}
