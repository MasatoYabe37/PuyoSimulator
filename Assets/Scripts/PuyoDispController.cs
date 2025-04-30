using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuyoDispController : MonoBehaviour
{
    public PuyoController parent = null;
    public PuyoController child = null;

    private void Start()
    {
        parent.SetInvisible();
        child.SetInvisible();
    }

    public void SetPuyo(PuyoDropInfo info)
    {
        parent.puyoType = info.puyoType1;
        parent.Setup();

        child.puyoType = info.puyoType2;
        child.Setup();

        parent.SetVisible();
        child.SetVisible();
    }

    public void Remove()
    {
        parent.SetInvisible();
        child.SetInvisible();
    }
}
