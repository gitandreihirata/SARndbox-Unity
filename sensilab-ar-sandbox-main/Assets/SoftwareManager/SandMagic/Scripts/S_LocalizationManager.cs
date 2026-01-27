using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

public class S_LocalizationManager : MonoBehaviour
{

    //Funcao que retorna a String da Traducao
    public String LocateStringtoDatabase(string key)
    {
        string translation = "";
        var database = LocalizationSettings.StringDatabase;

        // Verificar se o Table Collection "Translations" existe
        if (database != null)
        {
            // Obter a tradução correspondente à chave
            var table = database.GetTableAsync("LocalizationTables").Result as StringTable;

            if (table != null)
            {
                // Verificar se a chave existe na tabela
                var entry = table.GetEntry(key);
                if (entry != null)
                {
                    translation = entry.LocalizedValue;
                }
                else
                {
                    Debug.LogWarning("Translation key not found: " + "");
                }
            }
            else
            {
                Debug.LogWarning("Table Collection 'Translations' not found.");
            }
        }
        else
        {
            Debug.LogWarning("String database not found.");
        }

        return translation;
    }

}