using UnityEngine;
using System.Collections.Generic;

// Структура для хранения статов на каждом уровне прокачки
[System.Serializable]
public class ActiveSkillStats
{
    public float damage = 100f;
    public float manaCost = 20f;
    public float cooldown = 2f;
    public float speed = 20f;
}

public class Fireball : MonoBehaviour
{
    [Header("Настройки по уровням (0 = 1 уровень)")]
    public ActiveSkillStats[] statsPerLevel;

    [Header("Настройки полета")]
    public float maxDistance = 500f;
    public AudioClip hitSound; 

    // Ссылки, которые передаст игрок
    [HideInInspector] public float calculatedDamage = 0f; // <--- Сюда придет ИТОГОВЫЙ урон
    [HideInInspector] public int currentSkillLevel = 1;
    [HideInInspector] public List<string> skillTags;
    [HideInInspector] public IGameplaySkillManager gameplayManager;

    private Vector3 startPosition;
    private DamagePopupSpawner damagePopupSpawner; 
    private ActiveSkillStats myStats; // Текущие статы этого уровня

    private void Start()
    {
        startPosition = transform.position;
        damagePopupSpawner = FindFirstObjectByType<DamagePopupSpawner>();

        // Определяем, какие статы использовать на основе уровня (с защитой от выхода за массив)
        int index = Mathf.Max(0, currentSkillLevel - 1);
        if (statsPerLevel != null && statsPerLevel.Length > 0)
        {
            index = Mathf.Min(index, statsPerLevel.Length - 1);
            myStats = statsPerLevel[index];
        }
        else
        {
            myStats = new ActiveSkillStats(); // Если забыли настроить в инспекторе, берем дефолтные
        }
    }

    private void Update()
    {
        if (myStats == null) return;

        // Используем скорость из статов
        transform.Translate(Vector3.forward * myStats.speed * Time.deltaTime);

        // Уничтожаем, если улетел слишком далеко
        if (Vector3.Distance(startPosition, transform.position) >= maxDistance) 
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. Игнорируем коллайдеры самого игрока и триггеры (например, зоны агро врагов)
        if (other.CompareTag("Player") || other.GetComponentInParent<ThirdPersonCharacter>() != null) 
            return;
            
        if (other.isTrigger) 
            return;

        // 2. Проверяем, враг ли это
        var enemy = other.GetComponentInParent<EnemyAI>(); 
        if (enemy != null)
        {
            // Наносим итоговый урон, который рассчитал игрок
            enemy.TakeDamage(calculatedDamage);

            if (damagePopupSpawner != null)
                damagePopupSpawner.ShowDamage(enemy.transform, calculatedDamage, false, DamageType.Magical);

            // Вызываем пассивки (поджог и тд) через менеджер
            if (gameplayManager != null && skillTags != null)
                gameplayManager.ApplyOnHitEffects(enemy, skillTags);
        }
        else
        {
            // Проверка на попадание по критической точке врага
            var critPoint = other.GetComponent<CritPointMarker>();
            if (critPoint != null && critPoint.owner != null)
            {
                critPoint.owner.TakeDamage(calculatedDamage);
                if (damagePopupSpawner != null)
                {
                    damagePopupSpawner.ShowDamage(critPoint.owner.transform, calculatedDamage, true, DamageType.Magical);
                }

                if (gameplayManager != null && skillTags != null)
                    gameplayManager.ApplyOnHitEffects(critPoint.owner, skillTags);
            }
        }

        // 3. Звук попадания (в оба наушника)
        if (hitSound != null)
        {
            GameObject audioObj = new GameObject("FireballHitSound");
            AudioSource source = audioObj.AddComponent<AudioSource>();
            source.clip = hitSound;
            source.spatialBlend = 0f; // 2D звук
            source.Play();
            Destroy(audioObj, hitSound.length);
        }

        // 4. Уничтожаем снаряд при любом столкновении со стеной, полом или врагом
        Destroy(gameObject);
    }
}