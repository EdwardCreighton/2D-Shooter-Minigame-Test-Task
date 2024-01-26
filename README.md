﻿# Введение

Это тестовое задание на позицию C# Developer в Thera Interactive.
Требовалось создать на ЯП C# игру "Тир" без использования готовых игровых движков.

Для разработки архитектуры геймплея был выбран легковесный ecs-фреймворк LeoEcs.
В качестве окна с рендером игры используется Windows Forms.

# Процесс разработки

На разработку было затрачено примерно 64 часа, 40 из которых ушли на исследования и эксперименты с Windows Forms,
так как до этих пор мне не доводилось работать с графикой вне игровых движков.
Остальное время - разработка архитектуры, движка, геймплея и тестирования.

У меня давно есть идея собрать свой игровой движок для создания 2D игр, особенностью которого будет интегрированный ecs фреймворк.
Данное тестовое задание стало для меня своеобразной пробой пера, поэтому я полностью погрузился в разработку и каждая сфера, каждая ее часть была для меня интересной.

# Комментарии по архитектуре

С самого начала я решил, что буду разрабатывать геймплейную часть в парадигме ecs, так как она гораздо больше, чем ООП подходит для игровой логики.
Хоть ООП и более интуитивный для большинства программистов, это все же Enterprise подход к разработке.
В играх чаще всего геймплей формируется за счет комбинаций: есть объекты, а на них лежат компоненты, и в зависимости от наличия тех или иных компонентов, объект может выполнять то или иное действие.

Правильный фреймворк не только предоставляет удобный инструментарий, но еще и является оптимизированным по памяти и скорости.

Свой проект я условно разделил на две части: Assets и Engine (почему условно, будет понятно в разделе [сложности](#Cложности)). Engine содержит скрипты, формирующие фундамент игры. Assets - вся геймплейная логика.

Благодаря ecs в классе `App` можно легко записать в нужном порядке системы, которые должны отработать в одном игровом цикле

```csharp
// В конструкторе класса создаем среду Ecs 
private void CreateEcsInfrastructure()
{
	_world = new EcsWorld();

	_systemsRoot = new EcsSystems(_world);
	_gameplaySystems = new EcsSystems(_world);
	_gameplayLoader.AssignSystems(_gameplaySystems);
	
	_systemsRoot
		.Add(_gameplaySystems)
		.Add(new PhysicsSystem())
		.Add(new RenderSystem())
		.Inject(this)
		.Init();
}

// Таймером на ивенте Tick вызываем метод обработки
private void GameLoopTick(object? sender, EventArgs e)
{
	if (!_running)
		return;
	
	_systemsRoot.Run();
	CheckGameResult();

	UpdateGameProgress();

	Invalidate();
}
```

Весь геймплей записывается в `_gameplaySystems` через `IGameplaySystemsLoader.AssignSystems(EcsSystems)`, который реализован в директории Assets

```csharp
namespace ShootingRangeMiniGame.Assets
{
	public class ShootingRangeMiniGameLoader : IGameplaySystemsLoader
	{
		// ...
		
		public void AssignSystems(EcsSystems gameplaySystems)
		{
			gameplaySystems
				.Add(new PlayerLoader())
				.Add(new TargetsLoader())

				.Add(new PlayerRotationSystem())
				.Add(new PlayerShootSystem())
				.Add(new SpawnProjectileSystem())

				.Add(new TargetsOnCollisionResolver())
				.Add(new ProjectilesOnCollisionResolver())
				.Add(new GameProgressSystem())

				.Add(new MovementSystem())

				.Inject(_dataProvider);
		}
	}
}
```

Также сразу видно, какая логика в каком порядке обрабатывается.

# Сложности

В разработке была лишь одна сложность - Windows Forms не подходят для игры в том виде, каком я ее себе представлял.
Поэтому пришлось выкручиваться.

Не было возможности создать `UISystem` наподобие `RenderSystem` - из компонентов сформировать UI и управлять им из геймплея.
Весь UI пришлось хардкодить в "движке", а управлять из геймплея заполняя структуру `GameProgressData`

```csharp
namespace ShootingRangeMiniGame.Engine.Core
{
	public struct GameProgressData
	{
		public bool GameResult;

		public int InitialTargetsCount;
		public int InitialTime;
		public int InitialBulletsCount;
		
		public float TimeLeft;
		public int TargetsLeft;
		public int FreeProjectiles;

		public int BulletsLeft;
		public bool WeaponReady;
	}
}
```

> **Вывод**
> 
>Windows Forms не подходит как фреймворк рендера для игрового generic движка.
>В своем будущем движке использовать другой фреймворк.

Еще я не понял, почему не работает графика в отдельном `PictureBox`. Я могу рисовать и видеть объекты только на графике основной формы.
Все это привело к необходимости создавать ряд костыльных решений, чтобы сделать приятный UI.

# Известные недоработки

Так как время была ограничено, а большую его часть я исследовал Windows Forms, то геймплей не доведен до того конца, который я изначально себе представлял.

---
> Не хватает ригидности

Я создал компонент `Collider` и систему `PhysicsSystem`, которая обрабатывает коллизии и добавляет на объекты компоненты `OnCollision`.
Вся обработка, ответ на коллизию происходит уже в геймплейной части. С одной стороны, это правильно. Но с другой - там также должна быть заложена и фундаментальная логика по типу трения и упругости.
И описывать такое поведение в каждой системе-обработчике коллизий неправильно.

Не хватило времени разработать компонент `RigidBody` и описать его обработку в `PhysicsSystem`.

---
> Мало линейной алгебры

В компоненте `Transform` позиция и вращение описаны не в матрице, а в отдельных переменных. Также не хватает параметра Scale.

Снова не хватило времени аккуратно это собрать.

---
> Нет визуальных эффектов

На финальной стадии разработки хотелось добавить простых визуальных эффектов: взрыв шара - резкое увеличение в размерах перед уничтожением; истощение пули - после каждого удара об стену пуля должна менять цвет и трястись.

Не успел придумать, как сделать шейдеры.

---
> Нет сортирующих слоев для рендера

Задача была с минимальным приоритетом, тоже не хватило на нее времени.

---
> Нет маски коллизий

Каждый коллайдер фиксирует столкновение с любым другим коллайдером. Поэтому, чтобы пули не застревали в игроке, на нем нет никаких коллайдеров.
А из-за этого шары пролетают сквозь пушку.
Мелочь, но неприятно.