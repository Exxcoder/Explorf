<div align="center">

  <img src="appoh.ico" alt="Explorf Logo" width="96" height="96" />

  <h1>Explorf</h1>

  <p><b>Быстрый менеджер рабочих пространств и сессий Проводника Windows</b></p>

  <p>Забудьте про ручной поиск и открытие десятков директорий перед началом работы. Одно нажатие — и все нужные папки открыты.</p>

  <p>
    <a href="#-основные-возможности">Возможности</a> •
    <a href="#-скриншоты">Скриншоты</a> •
    <a href="#-стек-технологий">Стек технологий</a> •
    <a href="#-быстрый-старт">Быстрый старт</a> •
    <a href="#-как-это-работает">Как это работает</a>
  </p>

  <p>
    <img src="https://img.shields.io/badge/Language-C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white" alt="C#" />
    <img src="https://img.shields.io/badge/Framework-WPF%20%2F%20.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET WPF" />
    <img src="https://img.shields.io/badge/Platform-Windows-0078D6?style=for-the-badge&logo=windows&logoColor=white" alt="Windows" />
    <img src="https://img.shields.io/badge/License-MIT-green?style=for-the-badge" alt="License" />
  </p>

</div>

---

## ⚡ Основные возможности

* **🚀 Запуск рабочей среды в 1 клик:** Отмечайте галочками только те папки, которые нужны для текущей задачи, и открывайте их одновременно.
* **⏱ Умная каскадная задержка (Smart Open Delay):** Папки открываются через асинхронные интервалы (`Task.Delay`), чтобы Windows успела зарегистрировать процесс Проводника и не «склеивала» окна хаотично.
* **🎨 Кастомный Fluent / Dark UI:**
  * Полностью бескаркасное окно (`WindowStyle="None"`, `AllowsTransparency="True"`) с возможностью перетаскивания за любую область.
  * Тёмная тема в стиле Zinc/Modern Slate с аккуратными эффектами наведения.
  * Ультратонкие плавные кастомные скроллбары (попиксельная прокрутка).
  * Кастомные компоненты: стилизованные чекбоксы, карточки и кнопки с микровзаимодействиями.
* **💾 Автосохранение и портативность:** Все добавленные пути и состояние чекбоксов автоматически сохраняются в `%AppData%/Explorf/folders.xml`.
* **🧹 Автоматическое завершение:** Приложение открывает нужный набор папок и сразу самостоятельно закрывается (`Application.Current.Shutdown()`), не засоряя оперативную память и панель задач.

---

## 📸 Скриншоты

<div align="center">
  <!-- Замените ссылку ниже на ваш реальный скриншот или GIF -->
  <img src="https://via.placeholder.com/680x520/18181B/FFFFFF?text=Explorf+Interface+Preview" alt="Интерфейс Explorf" width="600" />
</div>

---

## 🛠 Стек технологий

| Компонент | Технология | Описание |
| :--- | :--- | :--- |
| **Язык** | **C#** | Основная бизнес-логика и асинхронность (`async/await`) |
| **UI Фреймворк** | **WPF (XAML)** | Кастомная верстка без стандартных рамок ОС |
| **Диалоги** | **WinForms Interop** | Native `FolderBrowserDialog` для удобного выбора директорий |
| **Хранилище** | **XML Serialization** | Быстрая сериализация профилей в `folders.xml` |

---

## 🚀 Быстрый старт

### Требования
* Операционная система: **Windows 10 / 11**
* Установленный **.NET Framework 4.8** (или выше) / **.NET Core / .NET 6+**

### Сборка из исходников

1. Склонируйте репозиторий:
   ```bash
   git clone [https://github.com/your-username/Explorf.git](https://github.com/your-username/Explorf.git)
