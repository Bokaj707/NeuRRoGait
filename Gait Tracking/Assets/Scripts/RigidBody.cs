using UnityEngine;
using UnityEngine.UI;
using System.Xml;
using System;
//=============================================================================----
// Copyright © NaturalPoint, Inc. All Rights Reserved.
// 
// This software is provided by the copyright holders and contributors "as is" and
// any express or implied warranties, including, but not limited to, the implied
// warranties of merchantability and fitness for a particular purpose are disclaimed.
// In no event shall NaturalPoint, Inc. or contributors be liable for any direct,
// indirect, incidental, special, exemplary, or consequential damages
// (including, but not limited to, procurement of substitute goods or services;
// loss of use, data, or profits; or business interruption) however caused
// and on any theory of liability, whether in contract, strict liability,
// or tort (including negligence or otherwise) arising in any way out of
// the use of this software, even if advised of the possibility of such damage.
//=============================================================================----

// Attach Body.cs to an empty Game Object and it will parse and create visual
// game objects based on bone data.  Body.cs is meant to be a simple example 
// of how to parse and display skeletal data in Unity.

// In order to work properly, this class is expecting that you also have instantiated
// another game object and attached the Slip Stream script to it.  Alternatively
// they could be attached to the same object.

public class RigidBody : MonoBehaviour {
	
	GameObject SlipStreamObject;
    GameObject body;

    bool initialized;
    bool[] tracked;
    Text[] displays;
    bool lost;
    string objectName;

	// Use this for initialization
	void Start () 
	{
		SlipStreamObject = GameObject.Find("Optitrack");
		SlipStreamObject.GetComponent<SlipStream>().PacketNotification += new PacketReceivedHandler(OnPacketReceived);
        tracked = null;
	}
	
	// packet received
    void OnPacketReceived(object sender, string Packet)
    {
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(Packet);

        XmlNodeList rigidBodyList = xmlDoc.GetElementsByTagName("Body");

        if(tracked == null)
        {
            tracked = new bool[rigidBodyList.Count];
            displays = new Text[rigidBodyList.Count];
            int i = 0;
            foreach(bool b in tracked)
            {
                tracked[i] = false;
                displays[i] = null;
            }
        }

        for (int index = 0; index < rigidBodyList.Count; index++)
        {
            string name = System.Convert.ToString(rigidBodyList[index].Attributes["Name"].InnerText);
            name = name.Replace(" ", string.Empty);
            name = name.ToLower();
            if (name.Equals("stylus", StringComparison.OrdinalIgnoreCase) == false && name.Equals("calibrationtool", StringComparison.OrdinalIgnoreCase) == false)
            {
                if (System.Convert.ToInt32(rigidBodyList[index].Attributes["Tracked"].InnerText) == 1)
                {
                    name = name.ToUpper();
                    body = GameObject.Find(name);
                    if(displays[index] == null)
                    {
                        displays[index] = GameObject.Find(name + "Tracking").GetComponent<Text>();
                    }
                    if (!tracked[index])
                    {
                        tracked[index] = true;
                        displays[index].color = Color.green;
                    }
                    if (body != null)
                    {
                        int id = System.Convert.ToInt32(rigidBodyList[index].Attributes["ID"].InnerText);

                        float x = (float)System.Convert.ToDouble(rigidBodyList[index].Attributes["x"].InnerText);
                        float y = (float)System.Convert.ToDouble(rigidBodyList[index].Attributes["y"].InnerText);
                        float z = (float)System.Convert.ToDouble(rigidBodyList[index].Attributes["z"].InnerText);

                        float qx = (float)System.Convert.ToDouble(rigidBodyList[index].Attributes["qx"].InnerText);
                        float qy = (float)System.Convert.ToDouble(rigidBodyList[index].Attributes["qy"].InnerText);
                        float qz = (float)System.Convert.ToDouble(rigidBodyList[index].Attributes["qz"].InnerText);
                        float qw = (float)System.Convert.ToDouble(rigidBodyList[index].Attributes["qw"].InnerText);

                        //== coordinate system conversion (right to left handed) ==--

                        z = -z;
                        qz = -qz;
                        qw = -qw;

                        //== bone pose ==--

                        Vector3 position = new Vector3(x, y, z);
                        Quaternion orientation = new Quaternion(qx, qy, qz, qw);

                        //== set bone's pose ==--

                        body.transform.position = position;
                        body.transform.rotation = orientation;
                    }
                }
                else if(tracked[index])
                {
                    displays[index].color = Color.red;
                    tracked[index] = false;
                }
            }
        }
    }
	// Update is called once per frame
	void Update () 
	{
	    
	}
}
