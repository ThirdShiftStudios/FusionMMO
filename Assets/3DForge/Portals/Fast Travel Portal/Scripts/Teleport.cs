using UnityEngine;
using System.Collections;

/// <summary>
/// Teleport.
/// Writen by Nico Schoeman on 7/23/2013.
/// Email: info@3dforge.co.za
/// 
/// teleports an object that touches the colider to the defined exit transform while spawning particles, and stoping the object from bouncing back and forth
/// </summary>

public class Teleport : MonoBehaviour {

	public Transform exit;
	
 
	void Start () {
		if (gameObject.GetComponent<Collider>().isTrigger == false) {
			gameObject.GetComponent<Collider>().isTrigger = true;
		}
	}
	
	void OnTriggerEnter(Collider other){
		other.transform.position = exit.transform.position;
		other.transform.rotation = exit.transform.rotation;
	}
	
}
