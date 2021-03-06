﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public abstract class UnitSkill : Skill {
    //public int level;
    public int costMP;
    public int costHP;
    public string description;
    public int skillRange;
    public int hoverRange;
    public int skillRate;
    protected SkillRange range;
    public Vector3 focus;
    private bool final;
    private GameObject confirmUI;
    protected bool rotateToPathDirection = true;
    protected Animator animator;
    protected GameObject comboJudgeUI;
    protected GameObject comboSelectUI;
    protected int animID;
    //originSkill是指组合技的第一个技能。
    public UnitSkill originSkill = null;
    //comboSkill是指组合技的第二个技能。
    public UnitSkill comboSkill  = null;
    protected GameObject render;

    private Dictionary<GameObject, PrivateItemData> buttonRecord = new Dictionary<GameObject, PrivateItemData>();

    public ComboType comboType;
    [Serializable]
    public enum ComboType
    {
        cannot,
        can,
        must
    }

    public SkillType skillType;
    [Serializable]
    public enum SkillType
    {
        attack,
        effect,
        defence,
        dodge,
    }
    public SkillClass skillClass;
    public enum SkillClass
    {
        ninjutsu,
        taijutsu,
        passive,
        tool,
        other,
    }

    public RangeType rangeType = RangeType.common;
    [Serializable]
    public enum RangeType
    {
        common,
        straight
    }

    public UnitSkill()
    {
        var unitSkillData = (UnitSkillData)skillData;
        costMP = unitSkillData.costMP;
        costHP = unitSkillData.costHP;
        description = unitSkillData.description;
        skillRange = unitSkillData.skillRange;
        hoverRange = unitSkillData.hoverRange;
        skillRate = unitSkillData.skillRate;
        comboType = unitSkillData.comboType;
        skillType = unitSkillData.skillType;
        skillClass = unitSkillData.skillClass;
        rangeType = unitSkillData.rangeType;
        animID = unitSkillData.animID;
    }

    public abstract void SetLevel(int level);

    public override bool Init(Transform character)
    {
        this.character = character;
        render = character.Find("Render").gameObject;
        if(skillType != SkillType.dodge)
        {
            //if (!CheckCost())
            //{
            //    Reset();
            //    return false;
            //}
        }

        //此处设定的是深度复制的技能实例。
        if(this is INinjaTool)
        {

        }
        else
        {
            SetLevel(character.GetComponent<CharacterStatus>().skills[_eName]);
        }

        animator = character.GetComponent<Animator>();
        if (originSkill == null)
        {
            GameObject go;
            switch (comboType)
            {
                case ComboType.cannot:
                    RangeInit();
                    break;
                case ComboType.can:
                    //继续使用连续攻击进行攻击吗？
                    go = (GameObject)Resources.Load("Prefabs/UI/Judge");
                    comboJudgeUI = UnityEngine.Object.Instantiate(go, GameObject.Find("Canvas").transform);
                    comboJudgeUI.name = "comboConfirmPanel";
                    comboJudgeUI.transform.Find("Text").GetComponent<Text>().text = "继续使用连续攻击进行攻击吗？";
                    comboJudgeUI.transform.Find("No").GetComponent<Button>().onClick.AddListener(RangeInit);
                    comboJudgeUI.transform.Find("Yes").GetComponent<Button>().onClick.AddListener(CreateComboSelectUI);
                    break;
                case ComboType.must:
                    //请选择要使用连续攻击的攻击类术·忍具。
                    go = (GameObject)Resources.Load("Prefabs/UI/Judge");
                    comboJudgeUI = UnityEngine.Object.Instantiate(go, GameObject.Find("Canvas").transform);
                    comboJudgeUI.name = "comboConfirmPanel";
                    comboJudgeUI.transform.Find("Text").GetComponent<Text>().text = "请选择要使用连续攻击的攻击类术·忍具。";
                    GameObject.Destroy(comboJudgeUI.transform.Find("No").gameObject);
                    var button = comboJudgeUI.transform.Find("Yes");
                    button.GetComponent<Button>().onClick.AddListener(CreateComboSelectUI);
                    button.localPosition = new Vector3(0, button.localPosition.y, button.localPosition.z);
                    break;
            }
        }
        else
        {
            originSkill.comboSkill = this;
            RangeInit();
        }
        return true;
    }

    protected void CreateComboSelectUI()
    {
        if (comboJudgeUI)
            GameObject.Destroy(comboJudgeUI);
        List<GameObject> allButtons;
        comboSelectUI = UIManager.GetInstance().CreateButtonList(character, this, out allButtons, ref buttonRecord, skill => { return skill.skillType == UnitSkill.SkillType.attack; });
        foreach (var button in allButtons)
        {
            button.GetComponent<Button>().onClick.AddListener(OnButtonClick);
        }
        comboSelectUI.transform.Find("Return").GetComponent<Button>().onClick.AddListener(Reset);
        comboSelectUI.SetActive(true);
    }

    protected void OnButtonClick()
    {
        var btn = EventSystem.current.currentSelectedGameObject;
        if (character.GetComponent<CharacterAction>().SetSkill(btn.name))
        {
            
            var skill = character.GetComponent<Unit>().action.Peek() as UnitSkill;
            skill.originSkill = this;
            if (comboSelectUI)
                UnityEngine.Object.Destroy(comboSelectUI);
            skillState = SkillState.reset;
        }
        else
        {
            
            character.GetComponent<CharacterAction>().SetItem(btn.name, buttonRecord[btn]);
            var skill = character.GetComponent<Unit>().action.Peek() as UnitSkill;
            skill.originSkill = this;
            if (comboSelectUI)
                UnityEngine.Object.Destroy(comboSelectUI);
            skillState = SkillState.reset;
        }
    }
    
    protected void RangeInit()
    {
        if (comboJudgeUI)
            GameObject.Destroy(comboJudgeUI);
        if(skillRange > 0)
        {
            range = new SkillRange();
            switch (rangeType)
            {
                case RangeType.common:
                    range.CreateSkillRange(skillRange, character);
                    break;
                case RangeType.straight:
                    range.CreateStraightSkillRange(skillRange, character);
                    break;
            }

            focus = new Vector3(-1, -1, -1);
            final = false;
            foreach (var f in BattleFieldManager.GetInstance().floors)
            {
                if (f.Value.activeInHierarchy)
                {
                    f.Value.GetComponent<Floor>().FloorClicked += Confirm;
                    f.Value.GetComponent<Floor>().FloorExited += DeleteHoverRange;
                    f.Value.GetComponent<Floor>().FloorHovered += Focus;
                }
            }
            //角色加入忽略层，方便选取
            UnitManager.GetInstance().units.ForEach(u => u.gameObject.layer = 2);

            range.RecoverColor();
        }
        else
        {
            Focus(character.gameObject, null);
            ShowConfirm();
        }
    }

    public override bool OnUpdate(Transform character)
    {
        switch (skillState)
        {
            case SkillState.init:
                if (Init(character))
                {
                    skillState = SkillState.waitForInput;
                }
                break;
            case SkillState.waitForInput:
                if (final)
                {
                    //角色取出忽略层
                    UnitManager.GetInstance().units.ForEach(u => u.gameObject.layer = 0);
                    
                    if (Check())
                    {
                        foreach (var f in BattleFieldManager.GetInstance().floors)
                        {
                            if (f.Value.activeInHierarchy)
                            {
                                f.Value.GetComponent<Floor>().FloorClicked -= Confirm;
                                f.Value.GetComponent<Floor>().FloorExited -= DeleteHoverRange;
                                f.Value.GetComponent<Floor>().FloorHovered -= Focus;
                            }
                        }
                        ShowConfirm();
                    }
                    UnitManager.GetInstance().units.ForEach(u => u.gameObject.layer = 2);
                    final = false;
                }
                break;
            case SkillState.confirm:
                
                if (originSkill != null)
                {
                    originSkill.InitSkill();
                }
                else
                {
                    InitSkill();
                }

                skillState = SkillState.applyEffect;
                
                break;
            case SkillState.applyEffect:
                if (ApplyEffects())
                {
                    animator.SetInteger("Skill", 0);
                    return true;
                }
                    
                break;
            case SkillState.reset:
                return true;
        }
        return false;
    }

    private void DeleteHoverRange(object sender, EventArgs e)
    {
        range.DeleteHoverRange();
        range.RecoverColor();
    }

    private void Focus(object sender, EventArgs e)
    {
        var go = sender as GameObject;
        focus = go.transform.position;
        if (originSkill != null)
            originSkill.focus = focus;
        if(skillRange > 0)
            range.ExcuteChangeColorAndRotate(hoverRange, skillRange, focus, rotateToPathDirection);
    }

    //AI
    public void Focus(Floor floor)
    {
        focus = floor.transform.position;
        range.ExcuteChangeColorAndRotate(hoverRange, skillRange, focus, rotateToPathDirection);
    }

    protected virtual void ShowConfirm()
    {
        var go = (GameObject)Resources.Load("Prefabs/UI/Confirm");
        confirmUI = UnityEngine.Object.Instantiate(go, GameObject.Find("Canvas").transform);
        confirmUI.transform.Find("Return").GetComponent<Button>().onClick.AddListener(ResetSelf);
        confirmUI.transform.Find("Confirm").GetComponent<Button>().onClick.AddListener(Confirm);
    }

    protected virtual void Confirm(object sender, EventArgs e)
    {
        final = true;
    }

    //confirmUI的回调函数
    protected virtual void Confirm()
    {
        if(confirmUI)
            UnityEngine.Object.Destroy(confirmUI);
        //角色取出忽略层
        UnitManager.GetInstance().units.ForEach(u => u.gameObject.layer = 0);
        skillState = SkillState.confirm;
    }

    protected virtual void InitSkill()
    {
        //连续技第一个。第二个在AttackSkill中。
        if (originSkill == null && comboSkill != null)
        {
            comboSkill.range.DeleteHoverRange();
            comboSkill.range.Delete();
            comboSkill.character.GetComponent<Unit>().action.Clear();  //禁止回退
            comboSkill.animator.SetInteger("Skill", animID);
        }
        if (originSkill == null && comboSkill == null)
        {
            if (range != null)
            {
                range.DeleteHoverRange();
                range.Delete();
            }
            character.GetComponent<Unit>().action.Clear();  //禁止回退
            animator.SetInteger("Skill", animID);
        }
        
    }

    //用来向技能面板输出本技能的效果和数值。长度为2或3。0位为Title，1位为Info,3位为DurationInfo。
    public virtual List<string> LogSkillEffect()
    {
        string title = "";
        string info = "";
        List<string> s = new List<string>
        {
            title,
            info
        };
        return s;
    }
    //ApplyEffects是一个时间段，这个时间段用来进行技能展示。
    protected abstract bool ApplyEffects();
    //Effect是一个时间点，由动画事件调用。
    public virtual void Effect()
    {
        Cost();
        animator.SetInteger("Skill", 0);
    }

    protected virtual void ResetSelf()
    {
        if (comboJudgeUI)
            GameObject.Destroy(comboJudgeUI);
        if (comboSelectUI)
            GameObject.Destroy(comboSelectUI);
        if (confirmUI)
            UnityEngine.Object.Destroy(confirmUI);
        UnitManager.GetInstance().units.ForEach(u => u.gameObject.layer = 0);
        if(range != null)
            range.Reset();

        foreach (var f in BattleFieldManager.GetInstance().floors)
        {
            f.Value.GetComponent<Floor>().FloorClicked -= Confirm;
            f.Value.GetComponent<Floor>().FloorExited -= DeleteHoverRange;
            f.Value.GetComponent<Floor>().FloorHovered -= Focus;
        }


        skillState = SkillState.init;
    }

    //与ResetSelf的区别：Reset在Skill层对技能进行出列入列，而ResetSelf仅用于类似替身术时候的自身重置。
    public override void Reset()
    {
        //按照顺序，逆序消除影响。因为每次会Init()，所以不必都Reset。
         
        if (range != null)
            range.Reset();
        
        foreach (var f in BattleFieldManager.GetInstance().floors)
        {
            f.Value.GetComponent<Floor>().FloorClicked -= Confirm;
            f.Value.GetComponent<Floor>().FloorExited -= DeleteHoverRange;
            f.Value.GetComponent<Floor>().FloorHovered -= Focus;
        }
        
        //角色取出忽略层
        UnitManager.GetInstance().units.ForEach(u => u.gameObject.layer = 0);
        if(comboJudgeUI)
            GameObject.Destroy(comboJudgeUI);
        if (comboSelectUI)
            GameObject.Destroy(comboSelectUI);
        if (confirmUI)
            UnityEngine.Object.Destroy(confirmUI);
        if(originSkill != null)
        {
            originSkill.ResetSelf();
            character.GetComponent<Unit>().action.Pop();
            
        }
        
        base.Reset();
    }

    public override bool Check()
    {
        return true;
    }
    
    public virtual bool Filter(Skill sender)
    {
        return CheckCost(sender.character, sender);
    }

    protected virtual bool CheckCost(Transform character, Skill sender)
    {
        var currentHP = character.GetComponent<CharacterStatus>().attributes.Find(d => d.eName == "hp").value;
        var currentMP = character.GetComponent<CharacterStatus>().attributes.Find(d => d.eName == "mp").value;
        if(sender is UnitSkill)
        {
            if (((UnitSkill)sender).costMP + costMP <= currentMP)
            {
                if (((UnitSkill)sender).costHP + costHP <= currentHP)
                {
                    return true;
                }
                else
                {
                    //DebugLogPanel.GetInstance().Log("体力不足！");
                    //Debug.Log("体力不足！");
                }
            }
            else
            {
                //DebugLogPanel.GetInstance().Log("查克拉不足！");
                //Debug.Log("查克拉不足！");
            }
        }
        else
        {
            if (costMP <= currentMP)
            {
                if (costHP <= currentHP)
                {
                    return true;
                }
                else
                {
                    //DebugLogPanel.GetInstance().Log("体力不足！");
                    //Debug.Log("体力不足！");
                }
            }
            else
            {
                //DebugLogPanel.GetInstance().Log("查克拉不足！");
                //Debug.Log("查克拉不足！");
            }
        }
        
        return false;
    }

    protected virtual void Cost()
    {
        var currentHP = character.GetComponent<CharacterStatus>().attributes.Find(d => d.eName == "hp").value;
        var currentMP = character.GetComponent<CharacterStatus>().attributes.Find(d => d.eName == "mp").value;

        var hp = currentHP - costHP;
        var mp = currentMP - costMP;
        ChangeData.ChangeValue(character, "hp", hp);
        ChangeData.ChangeValue(character, "mp", mp);
    }
}
