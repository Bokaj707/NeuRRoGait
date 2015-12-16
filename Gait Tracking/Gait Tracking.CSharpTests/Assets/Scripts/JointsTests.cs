using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnityEngine;

namespace Tests
{

    [TestClass()]
    public class JointsTests
    {
        [TestMethod()]
        public void FlexionExtensionTest()
        {
            GameObject knee = new GameObject();
            //GameObject kneePartner = new GameObject();
            //knee.transform.rotation = Quaternion.identity;
            //kneePartner.transform.rotation = Quaternion.Euler(new Vector3(0, 90f, 0));

            //Vector3 floatingKnee = Vector3.Cross(knee.transform.right, kneePartner.transform.up);

            //Joints j = new Joints();
            //float angle = j.FlexionExtension(knee, floatingKnee);
            //if(angle != 90)
           // {
                Assert.Fail();
            //}
        }
    }
}