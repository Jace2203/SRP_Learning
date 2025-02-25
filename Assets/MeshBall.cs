using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshBall : MonoBehaviour
{
    private const int NUM = 1023;

    private static int
        baseColorId     = Shader.PropertyToID("_BaseColor"),
        metallicId      = Shader.PropertyToID("_Metallic"),
        smoothnessId    = Shader.PropertyToID("_Smoothness");

    [SerializeField]
    private Mesh mesh = default;

    [SerializeField]
    private Material material = default;

    private Matrix4x4[] matrices = new Matrix4x4[NUM];
    private Vector4[] baseColors = new Vector4[NUM];
    private float[]
        metallic = new float[NUM],
        smoothness = new float[NUM];

    private MaterialPropertyBlock block;

    public void Awake()
    {
        for (int i = 0; i < NUM; i++)
        {
            matrices[i] = Matrix4x4.TRS(
                Random.onUnitSphere * 10f,
                Quaternion.identity,
                Vector3.one
            );
            baseColors[i] = new Vector4(
                Random.value,
                Random.value,
                Random.value,
                Random.Range(0.5f, 1f));
            metallic[i] = Random.value < 0.25f ? 1f : 0f;
            smoothness[i] = Random.Range(0.05f, 0.95f);
        }
    }

    public void Update()
    {
        if (block == null)
        {
            block = new MaterialPropertyBlock();
            block.SetVectorArray(baseColorId, baseColors);
            block.SetFloatArray(metallicId, metallic);
            block.SetFloatArray(smoothnessId, smoothness);
        }
        Graphics.DrawMeshInstanced(mesh, 0, material, matrices, NUM, block);
    }
}
