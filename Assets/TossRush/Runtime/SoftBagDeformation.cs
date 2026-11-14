using UnityEngine;

namespace TossRush
{
    // A compliant bag approximation: impact energy loss, a yielding collider, and cached mesh deformation.
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class SoftBagDeformation : MonoBehaviour
    {
        [Range(0, .8f)] public float maximumCompression = .5f;
        [Range(0, 1)] public float reboundRetention = .55f;
        [Min(0)] public float maximumReboundSpeed = .22f;
        public float Compression { get; private set; }
        Rigidbody body;
        SphereCollider sphere;
        float restRadius, targetCompression, contactUntil, renderedCompression;
        Vector3 restCenter, contactNormal = Vector3.up;
        MeshFilter[] filters;
        Mesh[] meshes;
        Vector3[][] originalVertices, rootVertices, deformedVertices;

        void Awake()
        {
            body = GetComponent<Rigidbody>(); sphere = GetComponent<SphereCollider>();
            restRadius = sphere.radius; restCenter = sphere.center;
            filters = GetComponentsInChildren<MeshFilter>(true);
            meshes = new Mesh[filters.Length]; originalVertices = new Vector3[filters.Length][];
            rootVertices = new Vector3[filters.Length][]; deformedVertices = new Vector3[filters.Length][];
            for (int i = 0; i < filters.Length; i++)
            {
                meshes[i] = Instantiate(filters[i].sharedMesh); meshes[i].name = "Soft Bag Instance";
                meshes[i].MarkDynamic(); filters[i].sharedMesh = meshes[i];
                originalVertices[i] = meshes[i].vertices;
                rootVertices[i] = new Vector3[originalVertices[i].Length];
                deformedVertices[i] = new Vector3[originalVertices[i].Length];
                Matrix4x4 toRoot = transform.worldToLocalMatrix * filters[i].transform.localToWorldMatrix;
                for (int v = 0; v < originalVertices[i].Length; v++) rootVertices[i][v] = toRoot.MultiplyPoint3x4(originalVertices[i][v]);
            }
            ResetDeformation();
        }

        public void ResetDeformation()
        {
            Compression = targetCompression = 0; contactUntil = float.NegativeInfinity;
            renderedCompression = 0;
            if (!sphere) return;
            sphere.radius = restRadius; sphere.center = restCenter;
            for (int i = 0; i < meshes.Length; i++)
            {
                meshes[i].vertices = originalVertices[i]; meshes[i].RecalculateNormals(); meshes[i].RecalculateBounds();
            }
        }

        public void AbsorbCollision(Collision collision)
        {
            var contact = collision.GetContact(0);
            contactNormal = contact.normal.normalized;
            contactUntil = Time.fixedTime + .08f;
            float impact = Mathf.Abs(Vector3.Dot(collision.relativeVelocity, contactNormal));
            targetCompression = Mathf.Max(targetCompression, maximumCompression * Mathf.Lerp(.65f, 1, Mathf.Clamp01(impact / 7)));
            Vector3 surfaceVelocity = collision.rigidbody ? collision.rigidbody.GetPointVelocity(contact.point) : Vector3.zero;
            float outward = Vector3.Dot(body.linearVelocity - surfaceVelocity, contactNormal);
            if (outward > 0)
                body.linearVelocity -= contactNormal * (outward - Mathf.Min(outward * reboundRetention, maximumReboundSpeed));
            body.angularVelocity *= .7f;
        }

        void OnCollisionStay(Collision collision)
        {
            if (collision.contactCount == 0) return;
            contactNormal = collision.GetContact(0).normal.normalized;
            contactUntil = Time.fixedTime + .08f;
            targetCompression = Mathf.Max(targetCompression, maximumCompression * .65f);
        }

        void FixedUpdate()
        {
            if (Time.fixedTime > contactUntil) targetCompression = Mathf.MoveTowards(targetCompression, 0, Time.fixedDeltaTime * .6f);
            Compression = Mathf.MoveTowards(Compression, targetCompression, Time.fixedDeltaTime * (targetCompression > Compression ? 10 : .8f));
            Vector3 axis = transform.InverseTransformDirection(contactNormal).normalized;
            sphere.radius = restRadius * (1 - Compression * .3f);
            // Align the collision support with the compressed underside instead of leaving the mesh floating.
            sphere.center = restCenter + axis * (sphere.radius - restRadius * (1 - Compression));
        }

        void LateUpdate()
        {
            if (Compression <= 0 && targetCompression <= 0)
            {
                if (renderedCompression > 0) ResetDeformation();
                return;
            }
            renderedCompression = Compression;
            Vector3 axis = transform.InverseTransformDirection(contactNormal).normalized;
            for (int i = 0; i < meshes.Length; i++)
            {
                Matrix4x4 toMesh = filters[i].transform.worldToLocalMatrix * transform.localToWorldMatrix;
                for (int v = 0; v < rootVertices[i].Length; v++)
                {
                    Vector3 p = rootVertices[i][v];
                    float along = Vector3.Dot(p - restCenter, axis);
                    Vector3 tangent = p - restCenter - axis * along;
                    Vector3 flattened = restCenter + tangent * (1 + Compression * .18f) + axis * (along * (1 - Compression));
                    deformedVertices[i][v] = toMesh.MultiplyPoint3x4(flattened);
                }
                meshes[i].vertices = deformedVertices[i]; meshes[i].RecalculateNormals(); meshes[i].RecalculateBounds();
            }
        }

        void OnDestroy()
        {
            if (meshes != null) foreach (var mesh in meshes) if (mesh) Destroy(mesh);
        }
    }
}
