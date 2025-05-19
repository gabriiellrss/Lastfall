// SurroundSpawner.cs
// Script para spawnar inimigos espalhados, mas bem próximos do jogador quando ele passa pelo trigger

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public class SurroundSpawner : MonoBehaviour
{
    [Header("Referências")]
    public GameObject enemyPrefab; // Prefab do inimigo a ser spawnado
    public Transform player; // Referência ao transform do jogador

    [Header("Configurações de Trigger")]
    public bool spawnOnce = true; // Se true, spawna apenas uma vez; se false, spawna toda vez que o jogador entrar
    public float spawnDelay = 0.5f; // Delay em segundos antes do spawn (para efeitos)
    public bool destroyTriggerAfterSpawn = false; // Se true, destrói o trigger após o spawn

    [Header("Configurações de Spawn")]
    public int numberOfEnemies = 3; // Número de inimigos para spawnar
    public float minDistanceFromPlayer = 15f; // Distância mínima do jogador (quão próximos)
    public float maxDistanceFromPlayer = 20f; // Distância máxima do jogador (quão espalhados)
    public bool facePlayer = true; // Se true, os inimigos olharão para o jogador ao spawnar
    public bool avoidFrontSpawn = false; // Se true, evita spawnar inimigos diretamente na frente do jogador

    [Header("Efeitos (Opcional)")]
    public GameObject spawnEffect; // Efeito visual para o spawn (opcional)
    public AudioClip spawnSound; // Som para tocar no spawn (opcional)

    private bool hasSpawned = false; // Controla se já spawnou (para spawnOnce)
    private AudioSource audioSource;
    private List<GameObject> spawnedEnemies = new List<GameObject>(); // Lista de inimigos spawnados

    void Start()
    {
        // Verifica se o enemyPrefab foi atribuído
        if (enemyPrefab == null)
        {
            Debug.LogError("Prefab do inimigo não atribuído no SurroundSpawner!");
            enabled = false;
            return;
        }

        // Encontra o jogador se não estiver atribuído
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
            else
            {
                Debug.LogError("Jogador não encontrado! Atribua o Transform do jogador ou marque-o com a tag 'Player'.");
                enabled = false;
                return;
            }
        }

        // Configura o AudioSource se houver um som de spawn
        if (spawnSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = spawnSound;
            audioSource.playOnAwake = false;
        }

        // Garante que o objeto tem um Collider configurado como trigger
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("SurroundSpawner precisa de um Collider! Adicione um BoxCollider, SphereCollider, etc.");
            enabled = false;
            return;
        }

        if (!col.isTrigger)
        {
            Debug.LogWarning("O Collider do SurroundSpawner não está configurado como trigger. Configurando automaticamente.");
            col.isTrigger = true;
        }
    }

    // Chamado quando outro collider entra no trigger
    void OnTriggerEnter(Collider other)
    {
        // Verifica se é o jogador que entrou no trigger
        if (other.CompareTag("Player"))
        {
            // Se configurado para spawnar apenas uma vez e já spawnou, não faz nada
            if (spawnOnce && hasSpawned)
                return;

            // Inicia o processo de spawn com o delay configurado
            StartCoroutine(SpawnEnemiesWithDelay());
        }
    }

    IEnumerator SpawnEnemiesWithDelay()
    {
        // Marca que já spawnou (para spawnOnce)
        hasSpawned = true;

        // Aguarda o delay configurado
        yield return new WaitForSeconds(spawnDelay);

        // Spawna os inimigos espalhados ao redor do jogador
        SpawnSurroundingEnemies();

        // Toca o som, se configurado
        if (audioSource != null && spawnSound != null)
        {
            audioSource.Play();
        }

        // Se configurado, destrói o trigger após o spawn
        if (destroyTriggerAfterSpawn)
        {
            Destroy(gameObject);
        }
    }

    void SpawnSurroundingEnemies()
    {
        List<Vector3> spawnPositions = new List<Vector3>();

        // Tenta encontrar posições válidas para todos os inimigos
        for (int i = 0; i < numberOfEnemies; i++)
        {
            Vector3 spawnPos = FindValidSpawnPosition(spawnPositions);
            if (spawnPos != Vector3.zero)
            {
                spawnPositions.Add(spawnPos);
            }
        }

        // Spawna os inimigos nas posições encontradas
        foreach (Vector3 position in spawnPositions)
        {
            // Calcula a rotação para o inimigo olhar para o jogador
            Quaternion lookRotation = facePlayer ?
                Quaternion.LookRotation((player.position - position).normalized) :
                Quaternion.identity;

            // Cria o efeito visual, se configurado
            if (spawnEffect != null)
            {
                Instantiate(spawnEffect, position, Quaternion.identity);
            }

            // Spawna o inimigo
            GameObject newEnemy = Instantiate(enemyPrefab, position, lookRotation);
            spawnedEnemies.Add(newEnemy);

            // Configura o inimigo para ser agressivo (opcional)
            SetupEnemyAggressive(newEnemy);

            // Pequeno delay entre spawns para evitar problemas de colisão
            System.Threading.Thread.Sleep(50);
        }
    }

    Vector3 FindValidSpawnPosition(List<Vector3> existingPositions)
    {
        // Número máximo de tentativas para encontrar uma posição válida
        int maxAttempts = 30;

        for (int i = 0; i < maxAttempts; i++)
        {
            // Gera um ângulo aleatório ao redor do jogador
            float randomAngle = Random.Range(0f, 360f);

            // Se configurado para evitar spawn na frente, verifica o ângulo
            if (avoidFrontSpawn)
            {
                // Evita ângulos entre -45 e 45 graus (na frente do jogador)
                float angleToForward = Mathf.Abs(Mathf.DeltaAngle(randomAngle, 0));
                if (angleToForward < 45)
                {
                    continue; // Tenta outro ângulo
                }
            }

            // Gera uma distância aleatória entre min e max
            float randomDistance = Random.Range(minDistanceFromPlayer, maxDistanceFromPlayer);

            // Calcula a direção a partir do ângulo
            Vector3 direction = Quaternion.Euler(0, randomAngle, 0) * Vector3.forward;

            // Calcula a posição potencial de spawn
            Vector3 potentialPosition = player.position + direction * randomDistance;

            // Verifica se a posição é válida na NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(potentialPosition, out hit, 2f, NavMesh.AllAreas))
            {
                // Verifica se está longe o suficiente de outras posições já escolhidas
                bool tooCloseToOther = false;
                foreach (Vector3 pos in existingPositions)
                {
                    if (Vector3.Distance(hit.position, pos) < 1.5f)
                    {
                        tooCloseToOther = true;
                        break;
                    }
                }

                if (!tooCloseToOther)
                {
                    return hit.position;
                }
            }
        }

        // Se não encontrou uma posição válida após várias tentativas
        Debug.LogWarning("Não foi possível encontrar uma posição válida para spawn após várias tentativas.");
        return Vector3.zero;
    }

    void SetupEnemyAggressive(GameObject enemy)
    {
        // Tenta obter o componente Enemy_v4 ou similar
        var enemyScript = enemy.GetComponent<MonoBehaviour>();

        // Aqui você pode configurar o estado do inimigo para perseguição/ataque
        // Isso depende da implementação específica do seu script de inimigo

        // Exemplo para Enemy_v4 (assumindo que tem um método ou propriedade para definir o estado):
        // if (enemyScript is Enemy_v4)
        // {
        //     (enemyScript as Enemy_v4).currentState = Enemy_v4.AIState.Chasing;
        // }

        // Como não temos acesso direto à implementação específica, deixamos um log
        Debug.Log("Inimigo configurado para ser agressivo. Ajuste o método SetupEnemyAggressive conforme necessário.");
    }

    // Desenha gizmos para visualizar a área do trigger e o spawn
    void OnDrawGizmos()
    {
        // Define a cor do gizmo para o trigger
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.5f); // Vermelho semi-transparente

        // Desenha um cubo ou esfera baseado no tipo de collider
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            if (col is BoxCollider)
            {
                BoxCollider boxCol = col as BoxCollider;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(boxCol.center, boxCol.size);
            }
            else if (col is SphereCollider)
            {
                SphereCollider sphereCol = col as SphereCollider;
                Gizmos.DrawSphere(transform.position + sphereCol.center, sphereCol.radius);
            }
        }
        else
        {
            // Se não tiver collider, desenha um cubo padrão
            Gizmos.DrawCube(transform.position, Vector3.one);
        }

        // Se o player estiver definido, desenha a visualização da área de spawn
        if (player != null)
        {
            // Desenha os círculos de distância mínima e máxima do jogador
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f); // Vermelho transparente
            DrawCircle(player.position, minDistanceFromPlayer, 32);

            Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // Verde transparente
            DrawCircle(player.position, maxDistanceFromPlayer, 32);

            // Se configurado para evitar spawn na frente, desenha o cone de visão
            if (avoidFrontSpawn)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f); // Amarelo transparente
                Vector3 leftDir = Quaternion.Euler(0, -45, 0) * player.forward;
                Vector3 rightDir = Quaternion.Euler(0, 45, 0) * player.forward;

                Gizmos.DrawLine(player.position, player.position + leftDir * maxDistanceFromPlayer);
                Gizmos.DrawLine(player.position, player.position + rightDir * maxDistanceFromPlayer);

                // Desenha um arco para representar a área de visão
                DrawArc(player.position, maxDistanceFromPlayer, -45, 45, 10);
            }
        }
    }

    // Método auxiliar para desenhar círculos no editor
    void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angle = 0f;
        float angleStep = 2f * Mathf.PI / segments;
        Vector3 previousPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

        for (int i = 0; i < segments + 1; i++)
        {
            angle += angleStep;
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }
    }

    // Método auxiliar para desenhar arcos no editor
    void DrawArc(Vector3 center, float radius, float startAngle, float endAngle, int segments)
    {
        float angleStep = (endAngle - startAngle) / segments;
        float angle = startAngle;

        Vector3 previousPoint = center + Quaternion.Euler(0, angle, 0) * Vector3.forward * radius;

        for (int i = 0; i < segments + 1; i++)
        {
            angle += angleStep;
            Vector3 nextPoint = center + Quaternion.Euler(0, angle, 0) * Vector3.forward * radius;
            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }
    }
}
