// Copyright (C) 2018 Creative Spore - All Rights Reserved
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CreativeSpore
{
    public class ProjectileBehaviour : MonoBehaviour 
    {
        [SerializeField]
        private BlockScheme m_blockScheme;

        public BlockScheme BlockScheme
        {
            get => m_blockScheme;
            set
            {
                m_blockScheme = value;
                GetComponent<MeshRenderer>().sharedMaterial = m_blockScheme?.BlockMaterial;
            }
        }

        public void Shoot(BlockBehaviour target, RaycastHit hitInfo)
        {
            StartCoroutine(ShootCO(target, hitInfo));
        }

        private IEnumerator ShootCO(BlockBehaviour target, RaycastHit hitInfo)
        {
            float shotTimeToHit = GameSettings.Default.Gameplay.shotTimeToHit;
            float shotBounceTime = GameSettings.Default.Gameplay.shotBounceTime;

            PlayParabolicTween(gameObject, target.transform.position, shotTimeToHit, 1f);

            yield return new WaitForSeconds(shotTimeToHit);

            if(!target)
            {
                this.PoolDestroy(gameObject);
            }
            else if (target.BlockScheme == this.BlockScheme)
            {
                float dotProd = Vector3.Dot(target.transform.position - hitInfo.point, target.transform.forward);
                if (dotProd > 0)
                {
                    target.Explode(0f);
                }
                else
                {
                    target.RotateFace();
                }
                this.PoolDestroy(gameObject);
            }
            else
            {
                PlayParabolicTween(gameObject, transform.position + hitInfo.normal * 20f, shotBounceTime, 10f);
                this.PoolDestroy(gameObject, shotBounceTime);
            }
        }

        private void PlayParabolicTween(GameObject target, Vector3 endPosition, float time, float height)
        {
            target.transform.DOPath(
                new Vector3[]
                {
                    target.transform.position,
                    height * Vector3.up + (target.transform.position + endPosition) / 2f,
                    endPosition
                },
                time,
                PathType.CatmullRom
                );
        }
    }
}
