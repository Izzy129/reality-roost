using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using System;
using JetBrains.Annotations;

[System.Serializable]
public class NodeData
{
    public int nodeNumber;
    public Vector3 coordinates;
    public float velocityMagnitude;
    public Vector3 velocityComponents;
    public float pressure;
    
}

[RequireComponent(typeof(ParticleSystem))]
public class NodeVisualizer : MonoBehaviour
{
    [Header("Input Data")]
    public List<NodeData> nodesToVisualize = new List<NodeData>();

    [Header("Scaling")]
    public float visualizationScale = 10f;

    [Header("Particle Appearance")]
    public float particleSize = 0.02f;
    public Gradient velocityColorGradient;

    private ParticleSystem _particleSystem;
    private ParticleSystem.Particle[] particles;
    private float maxVelocity;

    [Header("Render Settings")]
    private ParticleSystemRenderer psr;
    //public ParticleSystemRenderMode renderMode = ParticleSystemRenderMode.Billboard;
    public Boolean onlyShowInterior = false;
    public Boolean showDirection = false;
    public Color colorOne = new Color(1f, 0f, 0f, 1f);
    public Color colorZero = new Color(1f, 1f, 1f, 1f);
    public float threshold = 1f;
    void Awake()
    {
        _particleSystem = GetComponent<ParticleSystem>();
        psr = GetComponent<ParticleSystemRenderer>();
        psr.renderMode = ParticleSystemRenderMode.Mesh;
        psr.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
    }

    void Start()
    {
        if (nodesToVisualize == null || nodesToVisualize.Count == 0)
        {
            Debug.LogWarning("NodeVisualizer: No node data provided.");
            return;
        }
        

        //BuildParticles();
    }

    public void VisualizeNodes()
    {
        //psr.renderMode = renderMode;
        int count = nodesToVisualize.Count;
        particles = new ParticleSystem.Particle[count];
        maxVelocity = getMaxMagnitude();

        float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;
        float velMagnitude = float.MinValue;
        foreach (var node in nodesToVisualize)
        {
            Vector3 p = node.coordinates;
            minX = Mathf.Min(minX, p.x);
            minY = Mathf.Min(minY, p.y);
            minZ = Mathf.Min(minZ, p.z);

            maxX = Mathf.Max(maxX, p.x);
            maxY = Mathf.Max(maxY, p.y);
            maxZ = Mathf.Max(maxZ, p.z);
            velMagnitude = Mathf.Max(velMagnitude, node.velocityComponents.x, node.velocityComponents.y, node.velocityComponents.z);
        }

        float maxDiff = Mathf.Max(
            maxX - minX,
            maxY - minY,
            maxZ - minZ
        );

        var main = _particleSystem.main;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.loop = false;
        main.startLifetime = Mathf.Infinity;
        main.startSpeed = 0f;
        main.maxParticles = count;
        main.simulationSpeed = 0f; 
        var emission = _particleSystem.emission;
        emission.enabled = false;

        _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _particleSystem.Clear();

        for (int i = 0; i < count; i++)
        {
            var node = nodesToVisualize[i];

            Vector3 norm = new Vector3(
                (node.coordinates.x - minX) / maxDiff,
                (node.coordinates.y - minY) / maxDiff,
                (node.coordinates.z - minZ) / maxDiff
            );

            Vector3 localPos = (norm - Vector3.one * 0.5f) * visualizationScale;

            ParticleSystem.Particle p = new ParticleSystem.Particle();
            p.position = localPos;
            if (showDirection == true)
            {
                p.rotation3D = Quaternion.FromToRotation(Vector3.up, node.velocityComponents).eulerAngles;
            }
            else
            {
                p.rotation3D = Vector3.zero;
            }

            if (node.velocityComponents == Vector3.zero && onlyShowInterior == true)
            {
                p.startSize = 0;
            }
            else
            {
                p.startSize = particleSize;
            }
            p.startLifetime = Mathf.Infinity;
            p.remainingLifetime = Mathf.Infinity;
            
            if (velocityColorGradient != null) //1f is used as a placeholder for maxVelocity for now
            {
                //float v01 = Mathf.InverseLerp(0f, velMagnitude, node.velocityComponents.magnitude);
                //p.startColor = velocityColorGradient.Evaluate(v01);
                p.startColor = new Color(
                    //Mathf.InverseLerp(0f, velMagnitude, node.velocityComponents.x),
                    getRedValue(Mathf.InverseLerp(0f, maxVelocity, node.velocityMagnitude)),
                    //Mathf.InverseLerp(0f, velMagnitude, node.velocityComponents.y),
                    getGreenValue(Mathf.InverseLerp(0f, maxVelocity, node.velocityMagnitude)),
                    //Mathf.InverseLerp(0f, velMagnitude, node.velocityComponents.z),
                    getBlueValue(Mathf.InverseLerp(0f, maxVelocity, node.velocityMagnitude)),
                    1f);
            }
            else
            {
                p.startColor = Color.white;
            }

            particles[i] = p;
        }

        _particleSystem.SetParticles(particles, particles.Length);
    }

    
    public void AddNodeData(int nodeNum, Vector3 coords, float velMag, Vector3 velComponents)
    {     
        NodeData newNode = new NodeData
        {
            nodeNumber = nodeNum,
            coordinates = coords,
            velocityMagnitude = velMag,
            velocityComponents = velComponents
        };

        nodesToVisualize.Add(newNode);
    }
    public void switchInteriorSettings() 
    {
        if(onlyShowInterior == true)
        {
            onlyShowInterior = false;
            Debug.Log("showing surface");
            return;
        }
        if(onlyShowInterior == false)
        {
            onlyShowInterior = true;
            Debug.Log("displaying interior only");
            return;
        }
    }

    public void switchRenderingMode()
    {
        if (showDirection == true)
        {
            showDirection = false;
            psr.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            particleSize = 0.02f;
            return;
        }
        if (showDirection == false)
        {
            showDirection = true;
            psr.mesh = Resources.Load<Mesh>("Cone");
            particleSize = 0.0015f;
            return;
        }
    }

    public float getRedValue(float lerpVal)
    {
        float slope = colorOne.r - colorZero.r;
        return lerpVal * slope + colorZero.r;
    }
    public float getBlueValue(float lerpVal)
    {
        float slope = colorOne.b - colorZero.b;
        return lerpVal * slope + colorZero.b;
    }
    public float getGreenValue(float lerpVal)
    {
        float slope = colorOne.g - colorZero.g;
        return lerpVal * slope + colorZero.g;
    }

    public float getMaxMagnitude()
    {
        List<float> magnitudes = new List<float>();
        foreach (var node in nodesToVisualize)
        {
            magnitudes.Add(node.velocityMagnitude);
        }
        return Mathf.Max(magnitudes.ToArray());
    }

}

