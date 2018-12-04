using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum RainStates { noRain, LightRain, HeavyRain, StormRain}
public class RainCloudController : MonoBehaviour {
    public Vector3 direction;
    public float speed;
    public RainStates currentRainState;
    private RainStates lastRainState;
    public ParticleSystem[] rainParticleSystems;
    private ParticleSystem currentRainParticleSystem;
    public float amountOfWater;

    [Header("Variables for thunder")]
    public GameObject thunderPrefab;
    private Coroutine thunderStrikingCoroutine;
    public bool isThunderActive;
    public float maxTimeBetweenThunder;
    public float minTimeBetweenThunder;
    private float thunderTimer;
    public float cloudSize;
	// Use this for initialization
	void Start () {
		if(rainParticleSystems.Length < 3)
        {
            Debug.Log("You need to have 3 particleSystems");
            throw new System.Exception("More ParticleSystems!");
        }

        currentRainParticleSystem = GetComponent<ParticleSystem>();
        this.transform.GetChild(0).localScale = new Vector3(cloudSize, 1, cloudSize);
        direction.Normalize();
	}
	
	// Update is called once per frame
	void Update () {
        this.transform.position += direction * speed * Time.deltaTime;
        UpdateState();

    }

    void UpdateState()
    {
        if(lastRainState != currentRainState)
        {
            currentRainParticleSystem.Stop();
            switch (currentRainState)
            {
                case RainStates.noRain:
                    currentRainParticleSystem = null;
                    break;
                case RainStates.LightRain:
                    currentRainParticleSystem = rainParticleSystems[0];
                    break;
                case RainStates.HeavyRain:
                    currentRainParticleSystem = rainParticleSystems[1];
                    break;
                case RainStates.StormRain:
                    currentRainParticleSystem = rainParticleSystems[2];
                    thunderStrikingCoroutine = StartCoroutine("RainDownThunder");
                    break;
                default:
                    break;
            }
            currentRainParticleSystem.Play();
        }
        lastRainState = currentRainState;
    }
    /// <summary>
    /// Coroutine to throw thunder at random times;
    /// </summary>
    /// <returns></returns>
    public IEnumerator RainDownThunder()
    {
        thunderTimer = minTimeBetweenThunder;
        while (isThunderActive)
        {
            thunderTimer -= Time.deltaTime;
            if(thunderTimer < 0)
            {
                ResetRandomThunderTime();
                GameObject thunder = Instantiate<GameObject>(thunderPrefab, Random.insideUnitSphere * (cloudSize/2) + this.transform.position, Quaternion.identity);
                thunder.transform.parent = this.transform;
            }
            yield return 1;
        }
    }

    private void ResetRandomThunderTime()
    {
        thunderTimer = Random.Range(minTimeBetweenThunder, maxTimeBetweenThunder);
    }
}
