using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

[Serializable]
public class BandMemberInterface : ISerializationCallbackReceiver
{
    [SerializeField] private Group _targetGroup;
    [SerializeField] private BandMember _bandMember;

    // Propriedade pública somente leitura - NÃO tem setter
    public Group targetGroup => _targetGroup;
    public BandMember bandMember 
    { 
        get => _bandMember;
        set
        {
            if (value != null && value.group != _targetGroup)
            {
                Debug.LogError($"Músico deve ser do grupo {_targetGroup}, não {value.group}!");
                return;
            }
            _bandMember = value;
        }
    }

    // Constructor - ÚNICA maneira de definir _targetGroup
    public BandMemberInterface(Group targetGroup)
    {
        this._targetGroup = targetGroup;
        this._bandMember = null;
    }

    // Constructor privado sem parâmetros para serialização
    private BandMemberInterface()
    {
        // Impede criação sem grupo alvo via new()
    }

    public void OnBeforeSerialize()
    {
        if (_bandMember != null && _bandMember.group != _targetGroup)
        {
            _bandMember = null;
            Debug.Log($"Você precisa selecionar um músico de {_targetGroup} para esta função!");
        }
    }

    public void OnAfterDeserialize()
    {
        // Nada aqui - a validação é feita no OnBeforeSerialize
    }
}
