using System;
using UnityEngine;

[System.Serializable]
public class QuestionData
{
    public string question;
    public string[] answers;
    public int correctAnswerIndex;
    public string explanation;
    
    public QuestionData(string q, string[] a, int correct, string exp = "")
    {
        question = q;
        answers = a;
        correctAnswerIndex = correct;
        explanation = exp;
    }
}

[CreateAssetMenu(fileName = "QuestionDatabase", menuName = "Hacking Terminal/Question Database")]
public class QuestionDatabase : ScriptableObject
{
    [Header("Sustainability Questions")]
    public QuestionData[] questions = new QuestionData[]
    {
        new QuestionData(
            "O que acontece quando servidores ficam ligados 24h sem necessidade?",
            new string[] {
                "Melhoram o desempenho",
                "Consomem energia à toa e emitem CO₂",
                "Evitam falhas técnicas",
                "Reduzem a poluição"
            },
            1,
            "Servidores desnecessariamente ligados consomem energia elétrica constantemente, contribuindo para emissões de CO₂."
        ),
        
        new QuestionData(
            "Qual é a melhor prática para reduzir o consumo energético em data centers?",
            new string[] {
                "Manter todos os servidores sempre ligados",
                "Usar virtualização e desligar servidores não utilizados",
                "Aumentar a temperatura do ar condicionado",
                "Instalar mais servidores físicos"
            },
            1,
            "A virtualização permite consolidar cargas de trabalho e desligar servidores físicos desnecessários."
        ),
        
        new QuestionData(
            "O que significa 'Green Computing'?",
            new string[] {
                "Usar apenas computadores verdes",
                "Programar apenas em linguagens 'verdes'",
                "Práticas sustentáveis no uso de tecnologia",
                "Computadores que funcionam com energia solar"
            },
            2,
            "Green Computing refere-se ao uso ambientalmente responsável de computadores e recursos relacionados."
        ),
        
        new QuestionData(
            "Qual o impacto ambiental do e-waste (lixo eletrônico)?",
            new string[] {
                "Não tem impacto significativo",
                "Contamina solo e água com metais pesados",
                "Apenas ocupa espaço nos aterros",
                "Melhora a qualidade do solo"
            },
            1,
            "O lixo eletrônico contém metais pesados tóxicos que podem contaminar o meio ambiente."
        )
    };
    
    public QuestionData GetRandomQuestion()
    {
        if (questions.Length == 0) return null;
        int randomIndex = UnityEngine.Random.Range(0, questions.Length);
        return questions[randomIndex];
    }
    
    public QuestionData GetQuestionByIndex(int index)
    {
        if (index < 0 || index >= questions.Length) return null;
        return questions[index];
    }
}

