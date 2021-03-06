﻿/**
* Copyright 2019 Michael Pollind
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*     http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Remoting.Proxies;
using UnityEngine;

namespace PandaEntertainer
{
    public class BodyPartContainerContainer
    {
        public enum PrefabType
        {
            ENTERTAINER
        }

        private readonly BodyPartsContainer _bodyContainer;

        private readonly Employee _employee;
        private readonly List<GameObject> _hairstyles = new List<GameObject>();
        private readonly List<GameObject> _heads = new List<GameObject>();

        private readonly GameObject _hider;
        private readonly List<GameObject> _legs = new List<GameObject>();

        private readonly List<GameObject> _torsos = new List<GameObject>();

        private EmployeeCostume _baseCostume;

        public BodyPartContainerContainer(EmployeeCostume baseCostume, string name, PrefabType type, GameObject hider)
        {
            _baseCostume = baseCostume;
            _hider = hider;
            _bodyContainer = ScriptableObject.CreateInstance<BodyPartsContainer>();
            _bodyContainer.name = name;

            _employee = AssetManager.Instance.getPrefab<Employee>(Prefabs.Entertainer);
        }

        public GameObject AddTorso(GameObject torso)
        {
            var m = Remap(_baseCostume.bodyPartsMale.getTorso(0), torso);
            _torsos.Add(m);
            return torso;
        }

        public GameObject AddHeads(GameObject head)
        {
            var m = Remap(_baseCostume.bodyPartsMale.getHead(0), head);
            _heads.Add(m);
            return head;
        }

        public GameObject AddLegs(GameObject leg)
        {
            var m = Remap(_baseCostume.bodyPartsMale.getLegs(0), leg);
            _legs.Add(m);
            return leg;
        }

        public GameObject AddHairstyles(GameObject hairstyle)
        {
            MeshRenderer meshRenderer = hairstyle.GetComponent<MeshRenderer>();

            Material material = new Material(ScriptableSingleton<AssetManager>.Instance.diffuseMaterial);
            material.mainTexture = meshRenderer.material.mainTexture;
            meshRenderer.sharedMaterial = material;

            _hairstyles.Add(hairstyle);//RemapMaterial(_baseCostume.bodyPartsMale.getHairstyle(0), hairstyle));
            return hairstyle;
        }

        private GameObject Remap(GameObject duplicator, GameObject mappedTo)
        {
            var go = Object.Instantiate(duplicator);
            go.transform.SetParent(_hider.transform);

            var skinnedMesh = go.GetComponentInChildren<SkinnedMeshRenderer>();
            var mappingMesh = mappedTo.GetComponentInChildren<SkinnedMeshRenderer>();

            if (skinnedMesh == null)
            {
                Debug.Log("does not have skinned mesh:" + mappedTo.name);
                return go;
            }

            var oldMapping = new Dictionary<int, string>();
            for (var x = 0; x < mappingMesh.bones.Length; x++) oldMapping.Add(x, mappingMesh.bones[x].name);

            var bp = new List<Matrix4x4>();
            var newMapping = new Dictionary<string, int>();
            for (var x = 0; x < skinnedMesh.bones.Length; x++)
            {
                newMapping.Add(skinnedMesh.bones[x].name, x);

                var t = go.transform.FindRecursive(skinnedMesh.bones[x].name);
                if (t != null)
                    bp.Add(t.worldToLocalMatrix * go.transform.localToWorldMatrix);
                else
                    bp.Add(skinnedMesh.sharedMesh.bindposes[x]);
            }

            var boneWeights = new List<BoneWeight>();
            for (var x = 0; x < mappingMesh.sharedMesh.boneWeights.Length; x++)
            {
                var tempWeight = new BoneWeight();
                tempWeight.boneIndex0 =
                    Remapper(mappingMesh.sharedMesh.boneWeights[x].boneIndex0, newMapping, oldMapping);
                tempWeight.boneIndex1 =
                    Remapper(mappingMesh.sharedMesh.boneWeights[x].boneIndex1, newMapping, oldMapping);
                tempWeight.boneIndex2 =
                    Remapper(mappingMesh.sharedMesh.boneWeights[x].boneIndex2, newMapping, oldMapping);
                tempWeight.boneIndex3 =
                    Remapper(mappingMesh.sharedMesh.boneWeights[x].boneIndex3, newMapping, oldMapping);

                tempWeight.weight0 = mappingMesh.sharedMesh.boneWeights[x].weight0;
                tempWeight.weight1 = mappingMesh.sharedMesh.boneWeights[x].weight1;
                tempWeight.weight2 = mappingMesh.sharedMesh.boneWeights[x].weight2;
                tempWeight.weight3 = mappingMesh.sharedMesh.boneWeights[x].weight3;
                boneWeights.Add(tempWeight);
            }

            var tempMesh = Object.Instantiate(skinnedMesh.sharedMesh);

            tempMesh.Clear();
            tempMesh.vertices = mappingMesh.sharedMesh.vertices;
            tempMesh.uv = mappingMesh.sharedMesh.uv;
            tempMesh.triangles = mappingMesh.sharedMesh.triangles;
            tempMesh.RecalculateBounds();
            tempMesh.normals = mappingMesh.sharedMesh.normals;
            tempMesh.tangents = mappingMesh.sharedMesh.tangents;

            tempMesh.boneWeights = boneWeights.ToArray();
            tempMesh.bindposes = bp.ToArray();
            skinnedMesh.sharedMesh = tempMesh;

            skinnedMesh.sharedMaterial = mappingMesh.material;

            return go;
        }


        private int Remapper(int index, Dictionary<string, int> newMapping, Dictionary<int, string> oldMapping)
        {
            var boneName = oldMapping[index];
            if (newMapping.ContainsKey(boneName))
                return newMapping[boneName];
            Debug.Log("can't find bone mapping:" + boneName);
            return 0;
        }

        private GameObject RemapMaterial(GameObject duplicator, GameObject mappedTo)
        {
            var skinnedMesh = duplicator.GetComponent<MeshRenderer>();
            var mappingMesh = mappedTo.GetComponent<MeshRenderer>();

            var material = Object.Instantiate(skinnedMesh.sharedMaterial);

            // material.shader.renderQueue

            material.mainTexture = mappingMesh.material.mainTexture;
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, new Color(0,0,0,0));
            t.Apply();
            material.SetTexture("_MaskTex", t);
            material.SetFloat("_Metallic", 0.0f);

            mappingMesh.sharedMaterial = material;
            return mappedTo;
        }


        public BodyPartsContainer Apply()
        {
            typeof(BodyPartsContainer)
                .GetField("torsos", BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(_bodyContainer, _torsos.ToArray());
            typeof(BodyPartsContainer)
                .GetField("heads", BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(_bodyContainer, _heads.ToArray());
            typeof(BodyPartsContainer)
                .GetField("legs", BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(_bodyContainer, _legs.ToArray());
            typeof(BodyPartsContainer)
                .GetField("hairstyles", BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(_bodyContainer, _hairstyles.ToArray());

            typeof(BodyPartsContainer)
                .GetField("accessories", BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(_bodyContainer, new WearableProduct[] { });
            typeof(BodyPartsContainer)
                .GetField("headItems", BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(_bodyContainer, new WearableProduct[] { });
            typeof(BodyPartsContainer)
                .GetField("faceItems", BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(_bodyContainer, new WearableProduct[] { });


            return _bodyContainer;
        }

        public void Dispose()
        {
        }
    }
}
