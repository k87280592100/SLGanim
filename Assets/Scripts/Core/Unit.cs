﻿using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

/// <summary>
/// Base class for all units in the game.
/// </summary>
public abstract class Unit : MonoBehaviour {
    public bool UnitEnd { get; private set; }

    public List<SLG.Attribute> attributes = new List<SLG.Attribute>();

    /// <summary>
    /// UnitClicked event is invoked when user clicks the unit. It requires a collider on the unit game object to work.
    /// </summary>
    public event EventHandler UnitClicked;
    /// <summary>
    /// UnitSelected event is invoked when user clicks on unit that belongs to him. It requires a collider on the unit game object to work.
    /// </summary>
    public event EventHandler UnitSelected;
    public event EventHandler UnitDeselected;
    /// <summary>
    /// UnitHighlighted event is invoked when user moves cursor over the unit. It requires a collider on the unit game object to work.
    /// </summary>
    public event EventHandler UnitHighlighted;
    public event EventHandler UnitDehighlighted;

    public event EventHandler UnitDestroyed;
    public event EventHandler UnitEnded;

    public Renderer[] rend;

    public List<IBuff> Buffs { get; private set; }

    
    public Stack<Skill> action = new Stack<Skill>();
    
    /// <summary>
    /// Indicates the player that the unit belongs to. Should correspoond with PlayerNumber variable on Player script.
    /// </summary>
    public int playerNumber;

    /// <summary>
    /// Method called after object instantiation to initialize fields etc. 
    /// </summary>
    public virtual void Initialize()
    {
        Buffs = new List<IBuff>();
        rend = GetComponentsInChildren<Renderer>();
    }

    /// <summary>
    /// Method is called at the start of each turn.
    /// </summary>
    public virtual void OnRoundStart()
    {
        UnitEnd = false;
    }

    /// <summary>
    /// Method is called at the end of each turn.
    /// </summary>
    public virtual void OnRoundEnd()
    {
        
    }

    //OnTurnStart在不管敌方还是我方Turn开始的时候都会调用。
    public virtual void OnTurnStart()
    {
        for (int i = 0; i < rend.Length; i++)
        {
            if (rend[i].material.shader.name == "Shader/ToonOutLine")
                rend[i].material.SetFloat("_Gray", 0f);
        }

        //应该在TurnStart，才能保证在轮到自己的时候，buff已经做过结算。0表示持续至下一个Turn开始（敌方的）。
        Buffs.FindAll(b => b.Duration == 0).ForEach(b => { b.Undo(transform); });
        Buffs.RemoveAll(b => b.Duration == 0);
        Buffs.ForEach(b => { b.Duration--; });
    }

    public virtual void OnTurnEnd()
    {
        
        
    }

    public virtual void OnUnitEnd()
    {
        UnitEnd = true;
        if(UnitEnded != null)
            UnitEnded.Invoke(this, null);
        for (int i = 0; i < rend.Length; i++)
        {
            if (rend[i].material.shader.name == "Shader/ToonOutLine")
                rend[i].material.SetFloat("_Gray", 1f);
        }
        action.Clear();
    }

    /// <summary>
    /// Method is called when units HP drops below 1.
    /// </summary>
    public virtual void OnDestroyed()
    {
        Debug.Log(transform.name + " is Dead!");
        UnitManager.GetInstance().units.Remove(this);
        if (UnitDestroyed != null)
            UnitDestroyed.Invoke(this, null);
        Destroy(gameObject);
    }

    

    protected virtual void OnMouseDown()
    {
        if (UnitClicked != null)
            UnitClicked.Invoke(this, new EventArgs());
    }

    protected virtual void OnMouseEnter()
    {
        if (UnitHighlighted != null)
            UnitHighlighted.Invoke(this, new EventArgs());
    }

    protected virtual void OnMouseExit()
    {
        if (UnitDehighlighted != null)
            UnitDehighlighted.Invoke(this, new EventArgs());
    }

    /// <summary>
    /// Method is called when unit is selected.
    /// </summary>
    public virtual void OnUnitSelected()
    {
        //SetState(new UnitStateMarkedAsSelected(this));
        if (UnitSelected != null)
        {
            UnitSelected.Invoke(this, new EventArgs());
        }
    }
    /// <summary>
    /// Method is called when unit is deselected.
    /// </summary>
    public virtual void OnUnitDeselected()
    {
        //SetState(new UnitStateMarkedAsFriendly(this));
        if (UnitDeselected != null)
            UnitDeselected.Invoke(this, new EventArgs());
        for (int i = 0; i < rend.Length; i++)
        {
            if (rend[i].material.shader.name == "Shader/ToonOutLine")
                rend[i].material.SetColor("_OutLineColor", new Color(0, 0, 0, 1));
        }
    }

}
