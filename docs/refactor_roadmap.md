# Refactor Roadmap

## Document Role

- Status: `execution plan`
- Scope: post-stabilization refactor work for documentation, UI, application, and tests
- Documentation index: [README.md](README.md)
- Architecture baseline: [ARCHITECTURE.md](instructions/ARCHITECTURE.md)

## الحالة الحالية

تحديث الحالة بتاريخ `2026-04-01`:

- `المرحلة 0` نُفذت عمليًا:
  - أضيفت خريطة وثائق مركزية في [README.md](README.md)
  - وُضحت ملكية الوثائق الحية وحدودها
  - صُنفت المواد المرجعية والأرشيفية وربطت بالمسارات الحية
- `المرحلة 1` نُفذت عمليًا:
  - فُصلت طبقة الواجهة حسب المجال إلى:
    - [administration.css](../src/BG.Web/wwwroot/css/administration.css)
    - [requests.css](../src/BG.Web/wwwroot/css/requests.css)
    - [approvals.css](../src/BG.Web/wwwroot/css/approvals.css)
    - [operations.css](../src/BG.Web/wwwroot/css/operations.css)
    - [dispatch.css](../src/BG.Web/wwwroot/css/dispatch.css)
    - [intake.css](../src/BG.Web/wwwroot/css/intake.css)
  - تقلص [site.css](../src/BG.Web/wwwroot/css/site.css) من ملف جامع كبير إلى طبقة عامة ومشتركة أصغر
  - نُقلت primitives المشتركة إلى [surface-primitives.css](../src/BG.Web/wwwroot/css/surface-primitives.css) وأزيلت قواعد `CSS` الميتة
- `المرحلة 2` نُفذت عمليًا:
  - أُعيد تنظيم [Services](../src/BG.Application/Services) و[Contracts/Services](../src/BG.Application/Contracts/Services) و[Contracts/Persistence](../src/BG.Application/Contracts/Persistence) حسب المجال
  - أُعيد ترتيب [DependencyInjection.cs](../src/BG.Application/DependencyInjection.cs) ليطابق حدود المجالات الجديدة
- `المرحلة 3` نُفذت عمليًا:
  - قُسمت اختبارات `Hosted` إلى ملفات مجال أصغر داخل [../tests/BG.UnitTests/Hosted](../tests/BG.UnitTests/Hosted)
  - بقيت المساعدات المشتركة في [HostedFlowTests.cs](../tests/BG.UnitTests/Hosted/HostedFlowTests.cs) و[HostedAppFactory.cs](../tests/BG.UnitTests/Hosted/HostedAppFactory.cs)
  - اختبارات `Hosted` لم تعد مجمعة في ملف طويل واحد
  - ثُبتت مجموعة smoke/regression سريعة عبر [HostedSmokeTests.cs](../tests/BG.UnitTests/Hosted/HostedSmokeTests.cs) مع أمر تشغيل واضح في [testing.instructions.md](../.github/instructions/testing.instructions.md)

المتبقي من هذا المسار أصبح تحسينات لاحقة منخفضة الأولوية، لا إعادة تنظيم أساسية مفتوحة.

## الهدف

هذه الخارطة لا تهدف إلى إعادة بناء `BG` أو تغيير الـ `stack`.

هدفها هو تنفيذ إعادة تنظيم آمنة ومنخفضة المخاطر بعد تثبيت الإنتاج، بحيث يصبح الكود أسهل في الفهم والصيانة والاختبار دون المساس بالسلوك التشغيلي الأساسي.

## المبادئ الحاكمة

- لا `rewrite`
- لا تغيير لـ `Razor Pages`
- لا تفكيك للمشاريع الخمسة الحالية
- لا تعديل سلوكي مقصود إلا إذا ظهر خلل فعلي أثناء التنظيف
- كل مرحلة يجب أن تبقى قابلة للتحقق عبر:
  - `dotnet build BG.sln`
  - `dotnet test BG.sln`

## ما لا يشمله هذا المسار

- تغيير المعمارية الأساسية المعتمدة في [ARCHITECTURE.md](instructions/ARCHITECTURE.md)
- تحويل النظام إلى `SPA`
- استبدال `Bootstrap`
- استبدال `PostgreSQL`
- إعادة تصميم جذرية لطبقة `OCR`

## مؤشرات الحاجة الحالية

- ملف [site.css](../src/BG.Web/wwwroot/css/site.css) أصبح كبيرًا ويجمع قواعد متعددة المجالات.
- مجلد [Services](../src/BG.Application/Services) مسطح أكثر من اللازم مقارنة بتوسع المجالات التشغيلية.
- مجلد [Hosted](../tests/BG.UnitTests/Hosted) كان يعتمد سابقًا على ملف اختبارات جامع طويل لسيناريوهات متعددة.
- بعض صفحات الإدارة والعمليات ما زالت كبيرة بما يكفي لتستفيد من استخراج إضافي إلى `partials` ومكونات أصغر.
- توجد عدة مستندات `.md` تشغيلية ومعمارية وتعليمية، وبعضها يحمل تعليمات متداخلة أو متقادمة جزئيًا إذا لم يتم توحيد مصدر الحقيقة بينها.

## المرحلة 0: توحيد المستندات والتعليمات

### الهدف

جعل وثائق المشروع نفسها جزءًا من البنية المنضبطة، بحيث تصبح التعليمات المعتمدة واضحة، غير متعارضة، وسهلة الاكتشاف.

### لماذا هذه المرحلة لازمة

في `BG` لا تمثل ملفات `.md` شرحًا جانبيًا فقط، بل تمثل:

- قرارات معمارية
- تعليمات تطوير محلي
- تعليمات اختبار
- تعليمات نشر وتشغيل
- خطط تنفيذ وتحول

لذلك فإن تركها دون توحيد ينتج تعارضًا بين "ما يقوله الكود" و"ما تقوله الوثائق".

### نطاق العمل

- حصر جميع مستندات التعليمات المرجعية داخل المشروع، خصوصًا:
  - [ARCHITECTURE.md](instructions/ARCHITECTURE.md)
  - [LOCAL_DEVELOPMENT.md](instructions/LOCAL_DEVELOPMENT.md)
  - [PRODUCTION_DEPLOYMENT.md](instructions/PRODUCTION_DEPLOYMENT.md)
  - [PRODUCTION_RUNBOOK.md](instructions/PRODUCTION_RUNBOOK.md)
  - [testing.instructions.md](../.github/instructions/testing.instructions.md)
  - [frontend_reconstruction_plan.md](frontend_reconstruction_plan.md)
  - [ocr_implementation_plan.md](ocr_implementation_plan.md)
- تصنيف الوثائق إلى:
  - `source of truth`
  - `execution plan`
  - `reference/archive`
- إزالة أو تقليل التكرار بين الوثائق التي تشرح نفس الشيء بطرق مختلفة.
- إضافة إحالات متبادلة واضحة بين الوثائق الأساسية بدل بقاء كل ملف معزولًا.
- وسم الوثائق المرجعية القديمة أو الأرشيفية بوضوح حتى لا تُقرأ كتعليمات حية.
- توحيد اللغة والنبرة والمصطلحات الأساسية بين الوثائق العربية والإنجليزية عند الحاجة.
- اعتماد قاعدة تشغيلية: أي تغيير سلوكي أو تشغيلي مهم يجب أن يحدّث الوثيقة المرجعية المرتبطة به في نفس الدفعة.

### مخرجات هذه المرحلة

- خريطة وثائق واضحة: ماذا يقرأ المطور أولًا، وماذا يعتمد، وماذا يعد أرشيفًا فقط
- تقليل التعارض بين الوثائق الأساسية
- تثبيت "مصدر الحقيقة" لكل محور:
  - المعمارية
  - التطوير المحلي
  - الاختبارات
  - النشر والإنتاج
  - الخطط التنفيذية

### معيار الإنجاز

- عدم وجود تعليمات تشغيلية متعارضة بين الوثائق الأساسية
- وضوح الفرق بين الوثائق الحية والوثائق الأرشيفية
- سهولة اكتشاف الوثيقة الصحيحة لكل موضوع دون تخمين
- ارتباط التوثيق الفعلي بحالة الكود الحالية، لا بحالة تاريخية سابقة

## المرحلة 1: تنظيم الواجهة

### الهدف

تقليل التشابك في طبقة العرض وجعل كل مساحة تشغيل أسهل في الصيانة دون تغيير شكل النظام جذريًا.

### نطاق العمل

- تقسيم [site.css](../src/BG.Web/wwwroot/css/site.css) إلى ملفات حسب المجال:
  - `administration`
  - `requests`
  - `approvals`
  - `operations`
  - `dispatch`
  - `intake`
- الإبقاء على [workspace-shell.css](../src/BG.Web/wwwroot/css/workspace-shell.css) و[surface-primitives.css](../src/BG.Web/wwwroot/css/surface-primitives.css) كطبقة مشتركة فقط.
- استخراج المقاطع الكبيرة من صفحات مثل:
  - [Users.cshtml](../src/BG.Web/Pages/Administration/Users.cshtml)
  - [Workflow.cshtml](../src/BG.Web/Pages/Administration/Workflow.cshtml)
  إلى `partials` أصغر وواضحة المسؤولية.
- توحيد قواعد التسمية للـ `CSS classes` حتى لا يستمر النمو غير المنضبط.

### معيار الإنجاز

- تقليص حجم [site.css](../src/BG.Web/wwwroot/css/site.css) بوضوح
- عدم تغير السلوك البصري أو تدفقات العمل الأساسية
- بقاء السلوك responsive سليمًا على المقاسات الأساسية

## المرحلة 2: تنظيم طبقة التطبيق

### الهدف

جعل طبقة الأعمال أسهل في التتبع والتنقل مع الحفاظ على العقود والسلوك الحاليين.

### نطاق العمل

- إعادة ترتيب [Services](../src/BG.Application/Services) إلى مجلدات حسب المجال:
  - `Requests`
  - `Approvals`
  - `Operations`
  - `Intake`
  - `Administration`
  - `Identity`
- إبقاء العقود العامة مستقرة ما أمكن.
- تجميع المساعدات و`DTOs` و`mappers` القريبة من كل مجال بدل استمرار التبعثر.
- إضافة أو تحسين `service registration extensions` إذا كان ذلك يقلل التشابك في التسجيل والاكتشاف.

### معيار الإنجاز

- وضوح حدود المجالات داخل `Application`
- عدم تغير السلوك أو التبعيات بين الطبقات
- بقاء مرجعيات المشاريع كما هي دون كسر للمعمارية الحالية

## المرحلة 3: تنظيم الاختبارات

### الهدف

تحسين قابلية صيانة واكتشاف الانحدارات دون تغيير استراتيجية الاختبار المعتمدة.

### نطاق العمل

- تقسيم [HostedFlowTests.cs](../tests/BG.UnitTests/Hosted/HostedFlowTests.cs) إلى ملفات حسب المجال:
  - `Administration`
  - `Requests`
  - `Approvals`
  - `Operations`
  - `Dispatch`
- الإبقاء على `HostedAppFactory` والمساعدات المشتركة في مكان موحد.
- توضيح حدود طبقات الاختبار داخل [BG.UnitTests](../tests/BG.UnitTests).
- تثبيت مجموعة smoke/regression قصيرة وواضحة للتشغيل السريع بعد أي تعديل UI.

### معيار الإنجاز

- سهولة العثور على سيناريو الاختبار المناسب
- تقليل الملفات الجامعة الطويلة
- بقاء `dotnet test BG.sln` أخضر بالكامل

## ترتيب التنفيذ المعتمد

1. تنظيم الواجهة
2. تنظيم طبقة التطبيق
3. تنظيم الاختبارات

يسبق هذا الترتيب مسار تأسيسي هو:

0. توحيد المستندات والتعليمات

وهذا مقصود لأن التوثيق في هذا المشروع ليس ملحقًا ثانويًا، بل جزء من الانضباط التشغيلي والمعماري.

## توقيت التنفيذ

- الأفضل تنفيذ هذا المسار بعد تثبيت الإنتاج أو خلال نافذة هادئة
- لا يخلط مع `go-live`
- لا يبدأ بالتوازي مع دفعة تشغيلية عالية الخطورة إلا عند الضرورة

## تعريف النجاح

يعد هذا المسار ناجحًا عندما تتحقق الشروط التالية مع نهاية كل مرحلة:

- `dotnet build BG.sln`
- `dotnet test BG.sln`
- عدم وجود تغيير وظيفي مقصود في التدفقات الأساسية
- تحسن ملموس في حدود المسؤولية وحجم الملفات الكبيرة وسهولة التنقل داخل الكود

## القرار الحالي

القرار المعتمد الآن هو:

- لا حاجة إلى إعادة هيكلة جذرية للمشروع
- توجد حاجة إلى توحيد مستندات التعليمات والمرجعيات الحية داخل المشروع
- توجد حاجة إلى `refactor` منظم ومنخفض المخاطر
- يبقى هذا المسار تابعًا لأولويات التشغيل والإنتاج، وليس سابقًا لها
