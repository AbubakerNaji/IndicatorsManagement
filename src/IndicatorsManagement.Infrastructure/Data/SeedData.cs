using IndicatorsManagement.Domain.Entities;
using IndicatorsManagement.Domain.Enums;

namespace IndicatorsManagement.Infrastructure.Data;

/// <summary>
/// Static seed data extracted from the official indicators guide document:
/// "دليل مؤشرات وزارة الاقتصاد والتجارة"
/// Contains 15 organizational entities and 120 indicators.
/// </summary>
public static class SeedData
{
    public static List<(Entity Entity, List<Indicator> Indicators)> GetEntitiesWithIndicators()
    {
        return
        [
            // 1. مكتب الأمن الغذائي
            (
                new Entity { NameAr = "مكتب الأمن الغذائي", NameEn = "Food Security Office", Type = EntityType.Bureau, Status = "active" },
                GetFoodSecurityIndicators()
            ),
            // 2. إدارة التفتيش وحماية المستهلك
            (
                new Entity { NameAr = "إدارة التفتيش وحماية المستهلك", NameEn = "Inspection and Consumer Protection Department", Type = EntityType.Administration, Status = "active" },
                GetInspectionIndicators()
            ),
            // 3. مكتب التعاون الدولي
            (
                new Entity { NameAr = "مكتب التعاون الدولي", NameEn = "International Cooperation Office", Type = EntityType.Bureau, Status = "active" },
                GetInternationalCooperationIndicators()
            ),
            // 4. مكتب دعم وتمكين المرأة
            (
                new Entity { NameAr = "مكتب دعم وتمكين المرأة", NameEn = "Women Empowerment and Support Office", Type = EntityType.Bureau, Status = "active" },
                GetWomenEmpowermentIndicators()
            ),
            // 5. مصلحة السجل التجاري
            (
                new Entity { NameAr = "مصلحة السجل التجاري", NameEn = "Commercial Registry Authority", Type = EntityType.Authority, Status = "active" },
                GetCommercialRegistryIndicators()
            ),
            // 6. مكتب الوكالات التجارية
            (
                new Entity { NameAr = "مكتب الوكالات التجارية", NameEn = "Commercial Agencies Office", Type = EntityType.Bureau, Status = "active" },
                GetCommercialAgenciesIndicators()
            ),
            // 7. مكتب العلامات التجارية
            (
                new Entity { NameAr = "مكتب العلامات التجارية", NameEn = "Trademarks Office", Type = EntityType.Bureau, Status = "active" },
                GetTrademarksIndicators()
            ),
            // 8. صندوق ضمان الائتمان
            (
                new Entity { NameAr = "صندوق ضمان الائتمان", NameEn = "Credit Guarantee Fund", Type = EntityType.Fund, Status = "active" },
                GetCreditGuaranteeIndicators()
            ),
            // 9. هيئة الإشراف على التأمين
            (
                new Entity { NameAr = "هيئة الإشراف على التأمين", NameEn = "Insurance Supervision Authority", Type = EntityType.Authority, Status = "active" },
                GetInsuranceIndicators()
            ),
            // 10. الهيئة العامة للمعارض
            (
                new Entity { NameAr = "الهيئة العامة للمعارض", NameEn = "General Authority for Exhibitions", Type = EntityType.Authority, Status = "active" },
                GetExhibitionsIndicators()
            ),
            // 11. هيئة تنمية الصادرات الليبية
            (
                new Entity { NameAr = "هيئة تنمية الصادرات الليبية", NameEn = "Libyan Export Development Authority", Type = EntityType.Authority, Status = "active" },
                GetExportDevelopmentIndicators()
            ),
            // 12. شبكة ليبيا للتجارة
            (
                new Entity { NameAr = "شبكة ليبيا للتجارة", NameEn = "Libya Trade Network", Type = EntityType.Network, Status = "active" },
                GetTradeNetworkIndicators()
            ),
            // 13. الهيئة العامة لتشجيع الاستثمار وشؤون الخصخصة
            (
                new Entity { NameAr = "الهيئة العامة لتشجيع الاستثمار وشؤون الخصخصة", NameEn = "General Authority for Investment Promotion and Privatization", Type = EntityType.Authority, Status = "active" },
                GetInvestmentIndicators()
            ),
            // 14. هيئة سوق المال الليبي
            (
                new Entity { NameAr = "هيئة سوق المال الليبي", NameEn = "Libyan Capital Market Authority", Type = EntityType.Authority, Status = "active" },
                GetCapitalMarketIndicators()
            ),
        ];
    }

    // ═══════════════════════════════════════════════════════════════
    // 1. مكتب الأمن الغذائي — F-01 to F-12 (12 indicators)
    // ═══════════════════════════════════════════════════════════════
    private static List<Indicator> GetFoodSecurityIndicators() =>
    [
        new()
        {
            Code = "F-01",
            NameAr = "مؤشر اكتفاء ذاتي من السلع الغذائية الأساسية",
            DefinitionAr = "نسبة إنتاج السلعة محلياً إلى إجمالي الاستهلاك المحلي منها (إنتاج + وارد - صادر).",
            CalculationMethodAr = "(كمية الإنتاج المحلي لسلعة أساسية / (كمية الإنتاج المحلي + كمية الواردات - كمية الصادرات)) × 100 (يمكن حسابه لسلة من السلع الرئيسية كالقمح، الشعير، اللحوم، الألبان، الزيوت).",
            UnitAr = "نسبة مئوية (%)",
            DataSourceAr = "وزارة الزراعة / الجمارك",
            ObjectiveAr = "قياس مدى اعتماد الدولة على الإنتاج المحلي في تأمين احتياجات��ا الغذائية.",
            PublicationFrequency = PublicationFrequency.Quarterly
        },
        new()
        {
            Code = "F-02",
            NameAr = "الفجوة الغذائية للسلع الأساسية",
            DefinitionAr = "الفرق بين إجمالي الاستهلاك المحلي والإنتاج المحلي من السلع الغذائية الأساسية (الكمية التي يتم استيرادها لتغطية العجز).",
            CalculationMethodAr = "إجمالي الاستهلاك المحلي - إجمالي الإنتاج المحلي (لكل سلعة أساسية، ويمكن تجميعها بقيمة نقدية).",
            UnitAr = "طن / دينار ليبي",
            DataSourceAr = "وزارة الزراعة / الجمارك",
            ObjectiveAr = "تحديد حجم العجز الغذائي الذي يجب تغطيته بالاستيراد أو زيادة الإنتاج.",
            PublicationFrequency = PublicationFrequency.Annual
        },
        new()
        {
            Code = "F-03",
            NameAr = "حجم المخزون الاستراتيجي من السلع الأساسية",
            DefinitionAr = "الكميات المتوفرة من السلع الغذائية الأساسية في المخازن الاستراتيجية (عامة وخاصة) وتغطيتها لعدد أيام الاستهلاك.",
            CalculationMethodAr = "(إجمالي الكميات المخزنة من سلعة / متوسط الاستهلاك اليومي من تلك السلعة) = عدد أيام التغطية.",
            UnitAr = "يوم / طن",
            DataSourceAr = "الجهات المسؤولة عن التخزين الاستراتيجي (مثل المطاحن والأعلاف / ديوان الحبوب)",
            ObjectiveAr = "قياس مدى جاهزية الدولة لمواجهة الطوارئ والأزمات وانقطاع سلاسل الإمداد.",
            PublicationFrequency = PublicationFrequency.Monthly
        },
        new()
        {
            Code = "F-04",
            NameAr = "مؤشر أسعار السلع الغذائية الأساسية",
            DefinitionAr = "متوسط التغير في أسعار سلة من السلع الغذائية الأساسية، كمؤشر على القدرة الشرائية للمواطن وتكلفة المعيشة.",
            CalculationMethodAr = "(متوسط سعر السلعة في الفترة الحالية / متوسط سعر نفس السلعة في فترة الأساس) × 100 (يمكن اعتماد مؤشر الرقم القياسي لأسعار المستهلك - قسم الغذاء).",
            UnitAr = "رقم قياسي / نسبة تغير",
            DataSourceAr = "إدارة الدراسات / وزارة الاقتصاد / المسوح الميدانية",
            ObjectiveAr = "مراقبة استقرار الأسعار وتكلفة الغذاء وتأ��يرها على الأمن الغذائي للأسر.",
            PublicationFrequency = PublicationFrequency.Monthly
        },
        new()
        {
            Code = "F-05",
            NameAr = "حجم واردات السلع الغذائية الأساسية",
            DefinitionAr = "إجمالي الكميات والقيمة المستوردة من السلع الغذائية الأساسية.",
            CalculationMethodAr = "مجموع كميات وقيم الواردات من السلع الغذائية المصنفة (حسب التعريفة الجمركية) خلال الفترة.",
            UnitAr = "طن / دينار ليبي",
            DataSourceAr = "مصلحة الجمارك",
            ObjectiveAr = "قياس مدى اعتماد الدولة على الأسواق الخارجية في تلبية الاحتياجات الغذائية.",
            PublicationFrequency = PublicationFrequency.Quarterly
        },
        new()
        {
            Code = "F-06",
            NameAr = "حجم صادرات السلع الغذائية الأساسية",
            DefinitionAr = "إجمالي الكميات والقيمة المصدرة من السلع الغذائية الأساسية (إن وجدت).",
            CalculationMethodAr = "مجموع كميات وقيم الصادرات من السلع الغذائية خلال الفترة.",
            UnitAr = "طن / دينار ليبي",
            DataSourceAr = "مصلحة الجمارك",
            ObjectiveAr = "قياس الفائض الإنتاجي القابل للتصدير ومساهمة القطاع الغذائي في التجارة الخارجية.",
            PublicationFrequency = PublicationFrequency.Quarterly
        },
        new()
        {
            Code = "F-07",
            NameAr = "عدد مشاريع الأمن الغذائي المستهدفة والتي تتم متابعتها",
            DefinitionAr = "عدد المشاريع والبرامج (في مجال الإنتاج المحلي، التخزين، سلاسل الإمداد) التي يتم متابعتها وتقييم أدائها من قبل المكتب.",
            CalculationMethodAr = "حصر عدد المشاريع (قيد التنفيذ أو المخطط لها) التي يتابعها المكتب ويقدم تقارير دورية عنها.",
            UnitAr = "مشروع / برنامج",
            DataSourceAr = "مكتب الأمن الغذائي",
            ObjectiveAr = "قياس حجم التدخلات والمشاريع الوطنية في قطاع الأمن الغذائي.",
            PublicationFrequency = PublicationFrequency.Semi_Annual
        },
        new()
        {
            Code = "F-08",
            NameAr = "عدد الدراسات الفنية والاقتصادية الصادرة",
            DefinitionAr = "عدد التقارير والدراسات والتحليلات التي أعدها المكتب حول موضوعات الأمن الغذائي (تحليل الفجوات، تقييم المخاطر، السياسات المقترحة).",
            CalculationMethodAr = "حصر عدد الدراسات والتقارير الفنية والاقتصادية التي تم إصدارها خلال الفترة.",
            UnitAr = "دراسة / تقرير",
            DataSourceAr = "مكتب الأمن الغذائي وإدارة الدراسات",
            ObjectiveAr = "قياس الإنتاج الفكري والمعرفي للمكتب ودعم صنع القرار بتحليلات موضوعية.",
            PublicationFrequency = PublicationFrequency.Semi_Annual
        },
        new()
        {
            Code = "F-09",
            NameAr = "عدد المبادرات والبرامج الترويجية المنفذة",
            DefinitionAr = "عدد الحملات والمبادرات والندوات التي تم تنفيذها للترويج لمشاريع الأمن الغذائي ونشر الوعي.",
            CalculationMethodAr = "حصر عدد الفعاليات والمبادرات التوعوية والترويجية المنفذة خلال الفترة.",
            UnitAr = "مبادرة / فعالية",
            DataSourceAr = "مكتب الأمن الغذائي (مكتب الإعلام)",
            ObjectiveAr = "نشر ثقافة الأمن الغذائي وتحفيز الاستثمار في القطاع.",
            PublicationFrequency = PublicationFrequency.Semi_Annual
        },
        new()
        {
            Code = "F-10",
            NameAr = "عدد حالات المخاطر التي تم تحليلها ورصدها",
            DefinitionAr = "عدد المخاطر (عالمية أو محلية) التي تم رصدها وتحليلها من قبل المكتب وإعداد خطط للتعامل معها (مثل الجفاف، ارتفاع الأسعار العالمية، انقطاع سلاسل الإمداد).",
            CalculationMethodAr = "حصر عدد تقارير تحليل المخاطر الصادرة أو الحالات التي تمت دراستها وتقييمها.",
            UnitAr = "تقرير / حالة",
            DataSourceAr = "مكتب الأمن الغذائي / إدارة النمذجة (وحدة تحليل المخاطر)",
            ObjectiveAr = "قياس مدى استعداد المكتب وجاهزيته للتعامل مع الأزمات الغذائية المحتملة.",
            PublicationFrequency = PublicationFrequency.Semi_Annual
        },
        new()
        {
            Code = "F-11",
            NameAr = "عدد الجهات المرتبطة بقاعدة البيانات الوطنية للأمن الغذائي",
            DefinitionAr = "عدد المؤسسات والهيئات (وزارات، جهات رقابية، مصلحة الجمارك، جهات دولية) التي يتم تبادل البيانات معها بشكل منتظم عبر قاعدة البيانات.",
            CalculationMethodAr = "حصر عدد الجهات التي تم ربطها إلكترونياً أو تبادل البيانات معها بشكل دوري.",
            UnitAr = "جهة",
            DataSourceAr = "مكتب الأمن الغذائي",
            ObjectiveAr = "قياس مدى التكامل المعلوماتي والتنسيق بين الجهات ذات العلاقة بالأمن الغذائي.",
            PublicationFrequency = PublicationFrequency.Annual
        },
        new()
        {
            Code = "F-12",
            NameAr = "مؤشر تنوع مصادر الاستيراد",
            DefinitionAr = "عدد الدول التي يتم استيراد السلع الغذائية الأساسية منها، كمؤشر على مرونة سلاسل الإمداد وتقليل مخاطر الاعتماد على مصدر واحد.",
            CalculationMethodAr = "حصر عدد الدول المصدرة لسلة السلع الغذائية الأساسية إلى ليبيا خلال الفترة.",
            UnitAr = "دولة",
            DataSourceAr = "مصلحة الجمارك",
            ObjectiveAr = "تقليل مخاطر انقطاع الإمدادات عن طريق تنوع مصادر الاستيراد.",
            PublicationFrequency = PublicationFrequency.Annual
        },
    ];

    // ═══════════════════════════════════════════════════════════════
    // 2. إدارة التفتيش وحماية المستهلك — D-01 to D-12 (12 indicators)
    // ═══════════════════════════════════════════════════════════════
    private static List<Indicator> GetInspectionIndicators() =>
    [
        new()
        {
            Code = "D-01",
            NameAr = "عدد حملات التفتيش المنفذة",
            DefinitionAr = "إجمالي عدد الحملات والجولات التفتيشية التي تم تنفيذها على الأسواق والمحال التجارية والمستودعات للتأكد من سلامة السلع والبضائع.",
            CalculationMethodAr = "حصر عدد الحملات التفتيشية المنفذة (المبرمجة + المفاجئة) خلال الفترة.",
            UnitAr = "حملة / جولة",
            DataSourceAr = "إدارة التفتيش وحماية المستهلك (سجلات الحملات)",
            ObjectiveAr = "قياس مدى الانتشار والرقابة على الأسواق.",
            PublicationFrequency = PublicationFrequency.Monthly
        },
        new()
        {
            Code = "D-02",
            NameAr = "عدد المحال التجارية التي تم التفتيش عليها",
            DefinitionAr = "إجمالي عدد المنشآت التجارية (محلات، شركات، مستودعات) التي شملتها أعمال التفتيش خلال الفترة.",
            CalculationMethodAr = "حصر عدد المحال والمنشآت التي تم زيارتها وتفتيشها فعلياً (قد يكون أكبر من عدد الحملات لأن الحملة الواحدة تشمل عدة محال).",
            UnitAr = "محل / منشأة",
            DataSourceAr = "إدارة التفتيش وحماية المستهلك (تقارير الحملات)",
            ObjectiveAr = "قياس مدى التغطية الرقابية للسوق التجاري.",
            PublicationFrequency = PublicationFrequency.Quarterly
        },
        new()
        {
            Code = "D-03",
            NameAr = "عدد السلع المخالفة المضبوطة",
            DefinitionAr = "عدد العينات أو الكميات (أو الوحدات) من السلع والبضائع التي تم ضبطها نتيجة وجود مخالفات (منتهية الصلاحية، مغشوشة، غير مطابقة للمواصفات).",
            CalculationMethodAr = "حصر إجمالي عدد (أو كمية) المواد والسلع المضبوطة خلال الفترة (يمكن قياسها بالوحدة، الكيلوغرام، اللتر، أو القيمة).",
            UnitAr = "عنصر / كغم / لتر / دينار",
            DataSourceAr = "إدارة التفتيش وحماية المستهلك (محاضر الضبط)",
            ObjectiveAr = "قياس حجم المخالفات في السوق ومدى فعالية الرقابة.",
            PublicationFrequency = PublicationFrequency.Quarterly
        },
        new()
        {
            Code = "D-04",
            NameAr = "عدد محاضر الضبط المحررة",
            DefinitionAr = "عدد المحاضر الرسمية التي تم تحريرها ضد المخالفين (تجار ومنشآت) وإحالتها للجهات المختصة (النيابة، لجان النظر في المخالفات).",
            CalculationMethodAr = "حصر عدد محاضر الضبط الإدارية أو القانونية المحررة خلال الفترة.",
            UnitAr = "محضر",
            DataSourceAr = "إدارة التفتيش وحماية المستهلك (سجل المحاضر)",
            ObjectiveAr = "قياس مستوى الاستجابة القانونية للمخالفات واتخاذ الإجراءات.",
            PublicationFrequency = PublicationFrequency.Quarterly
        },
        new()
        {
            Code = "D-05",
            NameAr = "عدد الشكاوى والبلاغات الواردة",
            DefinitionAr = "إجمالي عدد الشكاوى المقدمة من المستهلكين أو الجهات الرسمية بخصوص سلع أو خدمات مخالفة أو ضارة.",
            CalculationMethodAr = "حصر عدد الشكاوى والبلاغات المستلمة عبر جميع القنوات (هاتف، موقع إلكتروني، مكتب، خطابات) خلال الفترة.",
            UnitAr = "شكوى / بلاغ",
            DataSourceAr = "قسم خدمة المواطن / سجل الشكاوى",
            ObjectiveAr = "قياس مدى تفاعل المستهلكين مع الإدارة وثقتهم بدورها الرقابي.",
            PublicationFrequency = PublicationFrequency.Monthly
        },
        new()
        {
            Code = "D-06",
            NameAr = "نسبة الشكاوى التي تمت معالجتها",
            DefinitionAr = "مدى كفاءة الإدارة في الاستجابة والتفاعل مع شكاوى المستهلكين وحلها.",
            CalculationMethodAr = "(عدد الشكاوى التي تم البت فيها واتخاذ إجراء بشأنها / إجمالي عدد الشكاوى الواردة) × 100",
            UnitAr = "نسبة مئوية (%)",
            DataSourceAr = "إدارة التفتيش وحماية المستهلك (سجل متابعة الشكاوى)",
            ObjectiveAr = "قياس كفاءة وفعالية الإدارة في خدمة المستهلك.",
            PublicationFrequency = PublicationFrequency.Quarterly
        },
        new()
        {
            Code = "D-07",
            NameAr = "عدد أجهزة القياس والمكاييل والموازين التي تم معايرتها",
            DefinitionAr = "عدد أجهزة القياس والموازين (في الأسواق والمحال التجارية) التي تم فحصها ومعايرتها والتأكد من مطابقتها للمواصفات الفنية.",
            CalculationMethodAr = "حصر عدد الأجهزة التي تم فحصها ومعايرتها خلال الفترة (سواء بشكل دوري أو بناءً على بلاغات).",
            UnitAr = "جهاز",
            DataSourceAr = "إدارة التفتيش وحماية المستهلك (قسم الموازين والمقاييس)",
            ObjectiveAr = "ضمان سلامة ودقة أدوات القياس لحماية حقوق المستهلك والبائع.",
            PublicationFrequency = PublicationFrequency.Semi_Annual
        },
        new()
        {
            Code = "D-08",
            NameAr = "عدد برامج التوعية والإرشاد الاستهلاكي المنفذة",
            DefinitionAr = "عدد الأنشطة التوعوية (ندوات، برامج إعلامية، نشرات، حملات توعوية) التي تم تنفيذها لنشر ثقافة الاستهلاك الآمن وتعريف المستهلكين بحقوقهم.",
            CalculationMethodAr = "حصر عدد البرامج والفعاليات والمواد الإعلامية التوعوية المنفذة خلال الفترة.",
            UnitAr = "برنامج / نشاط",
            DataSourceAr = "إدارة التفتيش وحماية المستهلك (قسم التوعية)",
            ObjectiveAr = "نشر الوعي الاستهلاكي وتعزيز ثقافة حقوق المستهلك.",
            PublicationFrequency = PublicationFrequency.Semi_Annual
        },
        new()
        {
            Code = "D-09",
            NameAr = "عدد مأموري الضبط القضائي المعتمدين",
            DefinitionAr = "إجمالي عدد موظفي الإدارة (والإدارات التابعة) الحاصلين على صفة مأموري الضبط القضائي والمخولين بتحرير محاضر رسمية.",
            CalculationMethodAr = "العدد التراكمي للموظفين النشطين الذين صدرت لهم قرارات بصفة مأمور ضبط قضائي.",
            UnitAr = "مأمور",
            DataSourceAr = "إدارة الشؤون القانونية / إدارة الموارد البشرية",
            ObjectiveAr = "قياس القدرة القانونية للإدارة على تنفيذ مهامها الرقابية بفعالية.",
            PublicationFrequency = PublicationFrequency.Annual
        },
        new()
        {
            Code = "D-10",
            NameAr = "إجمالي الغرامات المالية المحصلة",
            DefinitionAr = "القيمة الإجمالية للغرامات التي تم تحصيلها نتيجة المخالفات المضبوطة (سواء بشكل مباشر أو بعد الأحكام).",
            CalculationMethodAr = "مجموع المبالغ المالية المحصلة من الغرامات خلال الفترة.",
            UnitAr = "دينار ليبي",
            DataSourceAr = "الإدارة المالية / القسم القانوني",
            ObjectiveAr = "قياس الأثر الاقتصادي للرقابة وردع المخالفين.",
            PublicationFrequency = PublicationFrequency.Annual
        },
        new()
        {
            Code = "D-11",
            NameAr = "عدد مكاتب التفتيش وحماية المستهلك في وحدات الإدارة المحلية المتابعة",
            DefinitionAr = "عدد المكاتب التابعة للإدارة (في البلديات والمراقبات) التي يتم متابعتها وتقييم أدائها بشكل منتظم.",
            CalculationMethodAr = "العدد التراكمي للمكاتب التي تقدم تقارير دورية وتخضع لإشراف فني من الإدارة المركزية.",
            UnitAr = "مكتب",
            DataSourceAr = "إدارة التفتيش وحماية المستهلك (قسم متابعة المكاتب)",
            ObjectiveAr = "قياس مدى الانتشار الجغرافي للرقابة ومدى التنسيق مع الوحدات المحلية.",
            PublicationFrequency = PublicationFrequency.Semi_Annual
        },
        new()
        {
            Code = "D-12",
            NameAr = "عدد حالات التنسيق مع منظمات المجتمع المدني",
            DefinitionAr = "عدد مرات التعاون والأنشطة المشتركة (ورش عمل، ندوات، برامج) التي تمت بالتنسيق مع جمعيات حماية المستهلك ومؤسسات المجتمع المدني.",
            CalculationMethodAr = "حصر عدد حالات التنسيق واللقاءات والبرامج المشتركة المنفذة خلال الفترة.",
            UnitAr = "حالة / نشاط",
            DataSourceAr = "إدارة التفتيش وحماية المستهلك (سجل التعاون)",
            ObjectiveAr = "تعزيز ال��راكة مع المجتمع المدني لنشر الوعي وحماية المستهلك.",
            PublicationFrequency = PublicationFrequency.Semi_Annual
        },
    ];

    // ═══════════════════════════════════════════════════════════════
    // 3. مكتب التعاون الدولي — IC-01 to IC-12 (12 indicators)
    // ═══════════════════════════════════════════════════════════════
    private static List<Indicator> GetInternationalCooperationIndicators() =>
    [
        new()
        {
            Code = "IC-01",
            NameAr = "عدد الاتفاقيات والمعاهدات الدولية التي تم دراستها ومراجعتها",
            DefinitionAr = "عدد مشاريع الاتفاقيات والمعاهدات الدولية (الثنائية أو متعددة الأطراف) التي قام المكتب بدراستها وتحليلها وإعداد موقف بشأنها بالتنسيق مع الجهات المعنية.",
            CalculationMethodAr = "حصر عدد الاتفاقيات والمعاهدات التي تسلمها المكتب وأعد تقارير فنية أو قانونية بشأنها خلال الفترة.",
            UnitAr = "اتفاقية / معاهدة",
            DataSourceAr = "مكتب التعاون الدولي (سجل الاتفاقيات)",
            ObjectiveAr = "ضمان مراجعة دقيقة للالتزامات الدولية وحماية المصالح الوطنية.",
            PublicationFrequency = PublicationFrequency.Semi_Annual
        },
        new()
        {
            Code = "IC-02",
            NameAr = "عدد الاجتماعات الدولية والإقليمية التي تم المشاركة فيها",
            DefinitionAr = "عدد المؤتمرات والاجتماعات والملتقيات الرسمية (حضورياً أو افتراضياً) التي مثلت الوزارة فيها بحضور رسمي.",
            CalculationMethodAr = "حصر عدد المشاركات الرسمية (مؤتمرات، اجتماعات وزارية، لجان مشتركة، ورش عمل دولية) خلال الفترة.",
            UnitAr = "مشاركة / اجتماع",
            DataSourceAr = "مكتب التعاون الدولي (تقارير المهام)",
            ObjectiveAr = "قياس مدى حضور الوزارة في المحافل الدولية وتعزيز التواصل الدبلوماسي.",
            PublicationFrequency = PublicationFrequency.Quarterly
        },
        new()
        {
            Code = "IC-03",
            NameAr = "عدد التقارير الصادرة عن الاجتماعات الدولية",
            DefinitionAr = "عدد التقارير الرسمية والمحاضر والملخصات التي أعدها المكتب عن الاجتماعات والمؤتمرات الدولية التي تم حضورها أو متابعتها.",
            CalculationMethodAr = "حصر عدد التقارير (التي تتضمن نتائج الاجتماعات، القرارات الصادرة، التوصيات) التي تم إعدادها وتوزيعها على الجهات المعنية.",
            UnitAr = "تقرير",
            DataSourceAr = "مكتب التعاون الدولي (أرشيف التقارير)",
            ObjectiveAr = "توثيق المشاركات الدولية ونشر المعرفة بالقرارات والتوصيات على الجهات المعنية.",
            PublicationFrequency = PublicationFrequency.Quarterly
        },
        new()
        {
            Code = "IC-04",
            NameAr = "عدد الشراكات الدولية والإقليمية المنشأة أو المطورة",
            DefinitionAr = "عدد اتفاقيات الشراكة، مذكرات التفاهم، أو برامج التعاون الجديدة التي تم توقيعها أو تفعيلها مع منظمات دولية أو دول شريكة.",
            CalculationMethodAr = "حصر عدد الشراكات وبرامج التعاون الجديدة التي تم إبرامها خلال الفترة.",
            UnitAr = "شراكة / مذكرة تفاهم",
            DataSourceAr = "مكتب التعاون الدولي",
            ObjectiveAr = "توسيع شبكة العلاقات الدولية وجلب الخبرات والتقنيات لتطوير قطاعات الوزارة.",
            PublicationFrequency = PublicationFrequency.Annual
        },
        new()
        {
            Code = "IC-05",
            NameAr = "عدد فرص المنح والتدريب الدولي المستفادة",
            DefinitionAr = "عدد المنح الدراسية والتدريبية (غير المشروطة) التي تم الحصول عليها لموظفي الوزارة من المنظمات العربية والدولية.",
            CalculationMethodAr = "حصر عدد فرص التدريب والمنح الوظيفية التي تم ترتيبها واستفاد منها موظفو الوزارة خلال الفترة.",
            UnitAr = "فرصة / منحة",
            DataSourceAr = "مكتب التعاون الدولي / الموارد البشرية",
            ObjectiveAr = "تطوير قدرات الموارد البشرية بالوزارة من خلال الخبرات والبرامج الدولية.",
            PublicationFrequency = PublicationFrequency.Semi_Annual
        },
        new()
        {
            Code = "IC-06",
            NameAr = "عدد المساهمات المالية المتابعة للوزارة في المنظمات الدولية",
            DefinitionAr = "عدد الاشتراكات والمساهمات المالية المستحقة على الوزارة للمنظمات والهيئات الدولية، ومدى متابعتها وتسويتها.",
            CalculationMethodAr = "حصر عدد المنظمات التي تتابع إدارتها المالية للمساهمات المستحقة والمدفوعة.",
            UnitAr = "مساهمة / منظمة",
            DataSourceAr = "مكتب التعاون الدولي (بالتنسيق مع إدارة المنظمات بوزارة الخارجية)",
            ObjectiveAr = "ضمان الالتزام بالالتزامات المالية الدولية وحماية عضوية الوزارة في المحافل الدولية.",
            PublicationFrequency = PublicationFrequency.Annual
        },
        new()
        {
            Code = "IC-07",
            NameAr = "عدد توصيات المنظمات الدولية التي تم متابعتها",
            DefinitionAr = "عدد القرارات والتوصيات الصادرة عن المنظمات والمؤتمرات الدولية التي تم توزيعها على الجهات المختصة ومتابعة تنفيذها.",
            CalculationMethodAr = "حصر عدد التوصيات التي تم إدراجها في خطط المتابعة وتلقي تقارير حول تنفيذها.",
            UnitAr = "توصية",
            DataSourceAr = "مكتب التعاون الدولي",
            ObjectiveAr = "ضمان تفعيل مخرجات المشاركات الدولية والاستفادة منها في تطوير السياسات المحلية.",
            PublicationFrequency = PublicationFrequency.Semi_Annual
        },
        new()
        {
            Code = "IC-08",
            NameAr = "عدد الزيارات الدولية الرسمية المنظمة",
            DefinitionAr = "عدد الزيارات الرسمية (الوفود القادمة إلى ليبيا أو المغادرة منها) التي تم تنظيمها وتنسيقها وإعداد برامجها من قبل المكتب.",
            CalculationMethodAr = "حصر عدد الزيارات الرسمية (على مستوى وزراء، وكلاء، خبراء) التي تم الإعداد لها خلال الفترة.",
            UnitAr = "زيارة",
            DataSourceAr = "مكتب التعاون الدولي (تقارير المهام)",
            ObjectiveAr = "تعزيز التبادل الدبلوماسي والفني مع الدول والمنظمات الشريكة.",
            PublicationFrequency = PublicationFrequency.Quarterly
        },
        new()
        {
            Code = "IC-09",
            NameAr = "عدد مشاريع التعاون الفني الدولية قيد التنفيذ",
            DefinitionAr = "عدد البرامج والمشاريع الفنية المنفذة بالتعاون مع جهات دولية (منظمات أممية، وكالات تنموية، دول مانحة) في مجالات عمل الوزارة.",
            CalculationMethodAr = "حصر عدد المشاريع النشطة التي يتم تنفيذها بتمويل أو خبرة دولية خلال الفترة.",
            UnitAr = "مشروع",
            DataSourceAr = "مكتب التعاون الدولي",
            ObjectiveAr = "قياس حجم الاستفادة من المساعدات الفنية الدولية في تطوير قطاعات الوزارة.",
            PublicationFrequency = PublicationFrequency.Semi_Annual
        },
        new()
        {
            Code = "IC-10",
            NameAr = "معدل تنفيذ خطة المشاركات الدولية",
            DefinitionAr = "مدى الالتزام بخطة المشاركات الدولية المعتمدة للوزارة.",
            CalculationMethodAr = "(عدد المشاركات المنفذة فعلياً / عدد المشاركات المخطط لها في الخطة) × 100",
            UnitAr = "نسبة مئوية (%)",
            DataSourceAr = "مكتب التعاون الدولي",
            ObjectiveAr = "قياس كفاءة التخطيط والتنفيذ للمشاركات الدولية.",
            PublicationFrequency = PublicationFrequency.Annual
        },
        new()
        {
            Code = "IC-11",
            NameAr = "عدد تقارير المتابعة الدورية الصادرة عن المكتب",
            DefinitionAr = "عدد التقارير الشهرية/الربع سنوية/السنوية التي يعدها المكتب عن أنشطته وإنجازاته.",
            CalculationMethodAr = "حصر عدد التقارير الدورية المرفوعة للإدارة العليا خلال الفترة.",
            UnitAr = "تقرير",
            DataSourceAr = "مكتب التعاون الدولي",
            ObjectiveAr = "ضمان الشفافية والتوثيق المنتظم لأعمال المكتب.",
            PublicationFrequency = PublicationFrequency.Quarterly
        },
        new()
        {
            Code = "IC-12",
            NameAr = "عدد الجهات المنسق معها محلياً",
            DefinitionAr = "عدد الوزارات والمصالح والهيئات الوطنية التي تم التنسيق معها لإعداد مواقف موحدة أو مشاركات مشتركة.",
            CalculationMethodAr = "حصر عدد الجهات المحلية التي تم التواصل والتنسيق معها بشأن الملفات الدولية خلال الفترة.",
            UnitAr = "جهة محلية",
            DataSourceAr = "مكتب التعاون الدولي",
            ObjectiveAr = "قياس مدى التكامل والتنسيق بين الجهات الوطنية في الملفات الدولية.",
            PublicationFrequency = PublicationFrequency.Semi_Annual
        },
    ];

    // ═══════════════════════════════════════════════════════════════
    // 4. مكتب دعم وتمكين المرأة — W-01 to W-04 (4 indicators)
    // ═══════════════════════════════════════════════════════════════
    private static List<Indicator> GetWomenEmpowermentIndicators() =>
    [
        new()
        {
            Code = "W-01",
            NameAr = "عدد البرامج التدريبية الموجهة للمرأة",
            DefinitionAr = "عدد الدورات التدريبية وورش العمل والمؤتمرات والندوات التي تم تنفيذها (أو الإشراف عليها) لرفع كفاءة وتأهيل المرأة في مجال عمل الوزارة.",
            CalculationMethodAr = "حصر عدد البرامج التدريبية المنفذة فعلياً (وليست المخطط لها فقط) خلال الفترة.",
            UnitAr = "برنامج تدريبي",
            DataSourceAr = "مكتب دعم وتمكين المرأة (سجلات البرامج المنفذة)",
            ObjectiveAr = "بناء قدرات المرأة وتطوير مهاراتها الوظيفية والمهنية.",
            PublicationFrequency = PublicationFrequency.Semi_Annual
        },
        new()
        {
            Code = "W-02",
            NameAr = "نسبة مشاركة المرأة في المناصب القيادية",
            DefinitionAr = "مدى تمثيل المرأة في مواقع صنع القرار والإدارة العليا داخل الوزارة والجهات التابعة لها.",
            CalculationMethodAr = "(عدد النساء اللواتي يشغلن مناصب قيادية (مدير إدارة، وكيل، مستشار، رئيس قسم) / إجمالي عدد شاغلي المناصب القيادية في الوزارة) × 100",
            UnitAr = "نسبة مئوية (%)",
            DataSourceAr = "إدارة الموارد البشرية (هيكل الوظائف القيادية)",
            ObjectiveAr = "التمكين الإداري للمرأة وتعزيز مشاركتها في صنع القرار.",
            PublicationFrequency = PublicationFrequency.Annual
        },
        new()
        {
            Code = "W-03",
            NameAr = "عدد رائدات الأعمال المدعومات",
            DefinitionAr = "عدد السيدات صاحبات المشاريع الصغرى والمتوسطة (أو الراغبات في بدء مشروع) اللواتي استفدن من خدمات المكتب (استشارات، توجيه، تنسيق مع جهات تمويل، تدريب متخصص).",
            CalculationMethodAr = "إجمالي عدد المستفيدات من خدمات الدعم (عدا التدريب العام) المقدمة من المكتب أو بالتنسيق معه خلال الفترة.",
            UnitAr = "رائدة أعمال / مستفيدة",
            DataSourceAr = "مكتب دعم وتمكين المرأة (سجل المستفيدات من الخدمات)",
            ObjectiveAr = "دعم ريادة الأعمال النسائية وتعزيز دور المرأة في النشاط الاقتصادي.",
            PublicationFrequency = PublicationFrequency.Semi_Annual
        },
        new()
        {
            Code = "W-04",
            NameAr = "عدد المبادرات الاقتصادية الخاصة بالمرأة",
            DefinitionAr = "عدد المشاريع والبرامج والمبادرات النوعية التي أطلقها المكتب أو شارك في إطلاقها لتعزيز التمكين الاقتصادي للمرأة.",
            CalculationMethodAr = "حصر عدد المبادرات (حملات توعوية، برامج تمويل بالتنسيق مع جهات خارجية، منصات تسويق لمنتجات النساء، اتفاقيات تعاون) المنفذة خلال الفترة.",
            UnitAr = "مبادرة / برنامج",
            DataSourceAr = "مكتب دعم وتمكين المرأة (سجل المشاريع والمبادرات)",
            ObjectiveAr = "تعزيز المشاركة الاقتصادية للمرأة وإدماجها في سوق العمل بشكل فاعل.",
            PublicationFrequency = PublicationFrequency.Annual
        },
    ];

    // ═══════════════════════════════════════════════════════════════
    // 5. مصلحة السجل التجاري — 1 to 7 (7 indicators)
    // ═══════════════════════════════════════════════════════════════
    private static List<Indicator> GetCommercialRegistryIndicators() =>
    [
        new()
        {
            Code = "CR-01",
            NameAr = "عدد الأسماء التجارية الممنوحة",
            DefinitionAr = "عدد الأسماء التجارية التي تم الموافقة عليها ومنحها للشركات الوطنية بعد التأكد من عدم تكرارها أو تشابهها مع أسماء قائمة.",
            CalculationMethodAr = "مجموع الأسماء التجارية الممنوحة (للمؤسسات والشركات الجديدة) خلال فترة معينة.",
            UnitAr = "اسم تجاري",
            DataSourceAr = "إدارة شؤون الشركات / قسم حجز الأسماء التجارية",
            ObjectiveAr = "توحيد الأسماء التجارية ومنع الازدواجية والتشابه حمايةً للمتعاملين.",
            PublicationFrequency = PublicationFrequency.Semi_Annual
        },
        new()
        {
            Code = "CR-02",
            NameAr = "عدد الشركات العامة المسجلة",
            DefinitionAr = "عدد الشركات المملوكة كلياً أو جزئياً (بما لا يقل عن 50%) للدولة، والتي تم قيدها لأول مرة (إصدار جديد) أو تم تجديد قيدها خلال الفترة.",
            CalculationMethodAr = "(عدد الشركات العامة الجديدة + عدد الشركات العامة التي جددت قيدها) خلال الفترة.",
            UnitAr = "شركة",
            DataSourceAr = "إدارة شؤون الشركات (سجل الشركات العامة)",
            ObjectiveAr = "حصر الشركات العامة ومتابعة وضعها القانوني، وقياس حجم مشاركة الدولة في النشاط الاقتصادي.",
            PublicationFrequency = PublicationFrequency.Semi_Annual
        },
        new()
        {
            Code = "CR-03",
            NameAr = "عدد الشركات المشتركة المسجلة",
            DefinitionAr = "عدد الشركات ذات رأس المال المشترك بين مستثمرين ليبيين وأجانب (أي شركة يوجد بها شريك أجنبي)، والتي تم قيدها أو تجديد قيدها.",
            CalculationMethodAr = "(عدد الشركات المشتركة الجديدة + عدد الشركات المشتركة التي جددت قيدها) خلال الفترة.",
            UnitAr = "شركة",
            DataSourceAr = "إدارة شؤون الشركات (سجل الشركات المشتركة)",
            ObjectiveAr = "قياس حجم الاستثمار المشترك (الأجنبي-المحلي) ومتابعة نموه وانسياب رؤوس الأموال.",
            PublicationFrequency = PublicationFrequency.Semi_Annual
        },
        new()
        {
            Code = "CR-04",
            NameAr = "عدد فروع الشركات الأجنبية المسجلة",
            DefinitionAr = "عدد الأذونات الممنوحة لفروع الشركات الأجنبية (ليست شركة ليبية مستقلة بل فرع لكيان أجنبي) ومكاتب التمثيل التجاري للعمل داخل ليبيا.",
            CalculationMethodAr = "(عدد الفروع الجديدة + عدد الفروع التي جددت أذوناتها) خلال الفترة.",
            UnitAr = "فرع / إذن",
            DataSourceAr = "إدارة شؤون الشركات (سجل الفروع الأجنبية)",
            ObjectiveAr = "تنظيم عمل الشركات الأجنبية وضمان امتثالها للقوانين المحلية، وجذب الاستثمار الأجنبي غير المباشر.",
            PublicationFrequency = PublicationFrequency.Semi_Annual
        },
        new()
        {
            Code = "CR-05",
            NameAr = "عدد محرري العقود المقيدين بالسجل",
            DefinitionAr = "إجمالي عدد المحررين والمخولين قانوناً بتأسيس الشركات والموثقين والمقيدين في السجل الخاص بهم لدى المصلحة.",
            CalculationMethodAr = "العدد التراكمي للمحررين النشطين (الذين لديهم قيد ساري المفعول) في نهاية الفترة.",
            UnitAr = "محرر عقود",
            DataSourceAr = "إدارة شؤون الشركات / قسم المحررين (السجل الخاص)",
            ObjectiveAr = "ضمان كفاءة واحترافية إجراءات التأسيس من خلال حصر وتنظيم المهنة.",
            PublicationFrequency = PublicationFrequency.Semi_Annual
        },
        new()
        {
            Code = "CR-06",
            NameAr = "عدد مكاتب السجلات التجارية المربوطة بالمنظومة المركزية",
            DefinitionAr = "عدد مكاتب السجل التجاري المنتشرة في البلديات والمراقبات (الفروع) التي تم ربطها إلكترونياً بقاعدة البيانات المركزية وتعمل بنظام موحد.",
            CalculationMethodAr = "(عدد المكاتب المربوطة فعلياً وتتبادل البيانات آنياً / إجمالي عدد المكاتب المستهدفة في الخطة) × 100 (يمكن عرضه كنسبة مئوية أو كعدد).",
            UnitAr = "مكتب / نسبة مئوية (%)",
            DataSourceAr = "إدارة تقنية المعلومات / إدارة الرقابة على المكاتب",
            ObjectiveAr = "التحول الرقمي، وتوحيد الإجراءات والبيانات على مستوى الدولة، وضمان دقة الإحصاءات.",
            PublicationFrequency = PublicationFrequency.Semi_Annual
        },
        new()
        {
            Code = "CR-07",
            NameAr = "إجمالي الإيرادات المحصلة",
            DefinitionAr = "القيمة المالية الإجمالية المحصلة نظير جميع الخدمات التي تقدمها المصلحة (رسوم حجز الاسم، التسجيل، التجديد، تعديل البيانات، وغيرها).",
            CalculationMethodAr = "مجموع المبالغ المالية المودعة في الخزينة العامة أو الحسابات البنكية للمصلحة خلال الفترة.",
            UnitAr = "دينار ليبي",
            DataSourceAr = "الإدارة المالية (قسم التحصيل / الحسابات)",
            ObjectiveAr = "قياس العائد المالي للدولة من النشاط التجاري الرسمي، وتعزيز استدامة المصلحة.",
            PublicationFrequency = PublicationFrequency.Semi_Annual
        },
    ];

    // ═══════════════════════════════════════════════════════════════
    // 6. مكتب الوكالات التجارية — CA-01 to CA-10 (10 indicators)
    // ═══════════════════════════════════════════════════════════════
    private static List<Indicator> GetCommercialAgenciesIndicators() =>
    [
        new() { Code = "CA-01", NameAr = "إجمالي طلبات الوكالات التجارية المقدمة", DefinitionAr = "عدد الطلبات الجديدة المقدمة إلى المكتب للحصول على إذن مزاولة نشاط وكالة تجارية (تأسيس جديد) خلال الفترة.", CalculationMethodAr = "حصر عدد طلبات التسجيل الجديدة (الواردة) خلال الفترة المحددة.", UnitAr = "طلب", DataSourceAr = "مكتب الوكالات التجارية", ObjectiveAr = "قياس حجم الطلب على نشاط الوكالات التجارية في السوق.", PublicationFrequency = PublicationFrequency.Quarterly },
        new() { Code = "CA-02", NameAr = "إجمالي الوكالات التجارية الممنوحة (صدرت بها قرارات)", DefinitionAr = "عدد الوكالات التجارية التي صدرت لها قرارات موافقة (تراخيص جديدة) من المكتب خلال الفترة.", CalculationMethodAr = "حصر عدد القرارات الإدارية الصادرة بالموافقة على طلبات الوكالات التجارية الجديدة خلال الفترة.", UnitAr = "وكالة / قرار", DataSourceAr = "مكتب الوكالات التجارية", ObjectiveAr = "قياس حجم التدفق الجديد للوكالات المرخصة في السوق.", PublicationFrequency = PublicationFrequency.Quarterly },
        new() { Code = "CA-03", NameAr = "إجمالي الوكالات التجارية المحلية الممنوحة", DefinitionAr = "عدد الوكالات التجارية الممنوحة (خلال الفترة) التي يكون فيها الموكل (الأصيل) طرفاً محلياً (ليبي الجنسية).", CalculationMethodAr = "حصر عدد الوكالات الممنوحة (من CA-02) التي يكون موكلها ليبياً (شخص طبيعي أو اعتباري وطني).", UnitAr = "وكالة محلية", DataSourceAr = "مكتب الوكالات التجارية", ObjectiveAr = "قياس نشاط الوكلاء المحليين وحجم تمثيلهم للعلامات المحلية.", PublicationFrequency = PublicationFrequency.Semi_Annual },
        new() { Code = "CA-04", NameAr = "إجمالي الوكالات التجارية الأجنبية الممنوحة", DefinitionAr = "عدد الوكالات التجارية الممنوحة (خلال الفترة) التي يكون فيها الموكل (الأصيل) طرفاً أجنبياً (غير ليبي).", CalculationMethodAr = "حصر عدد الوكالات الممنوحة (من CA-02) التي يكون موكلها أجنبياً (شركة أو شخص أجنبي).", UnitAr = "وكالة أجنبية", DataSourceAr = "مكتب الوكالات التجارية", ObjectiveAr = "قياس مدى جاذبية السوق الليبي للعلامات والمنتجات الأجنبية.", PublicationFrequency = PublicationFrequency.Semi_Annual },
        new() { Code = "CA-05", NameAr = "إجمالي الوكالات التجارية النشطة (السارية)", DefinitionAr = "العدد الإجمالي للوكالات التجارية المسجلة والتي لا تزال سارية المفعول (جديدة + مجددة - منتهية - ملغاة) في نهاية الفترة.", CalculationMethodAr = "(العدد التراكمي للوكالات الممنوحة سابقاً + الوكالات المجددة خلال الفترة) - (الوكالات المنتهية والملغاة خلال الفترة).", UnitAr = "وكالة نشطة", DataSourceAr = "مكتب الوكالات التجارية", ObjectiveAr = "حجم سوق الوكالات التجارية الفعلي والنشط في الاقتصاد.", PublicationFrequency = PublicationFrequency.Annual },
        new() { Code = "CA-06", NameAr = "عدد الوكالات التجارية المجددة", DefinitionAr = "عدد الوكالات التجارية القائمة التي تم تجديد تراخيصها (أذوناتها) خلال الفترة.", CalculationMethodAr = "حصر عدد طلبات التجديد التي تمت الموافقة عليها وإتمام إجراءاتها خلال الفترة.", UnitAr = "وكالة مجددة", DataSourceAr = "مكتب الوكالات التجارية", ObjectiveAr = "قياس مدى استمرارية واستقرار نشاط الوكالات القائمة.", PublicationFrequency = PublicationFrequency.Semi_Annual },
        new() { Code = "CA-07", NameAr = "إجمالي الإيرادات المحصلة من نشاط الوكالات التجارية", DefinitionAr = "القيمة المالية الإجمالية المحصلة نظير جميع الخدمات (رسوم دراسة الطلبات، رسوم القيد، رسوم التجديد، تعديل البيانات، وغيرها).", CalculationMethodAr = "مجموع المبالغ المالية المودعة في الخزينة العامة أو الحسابات البنكية للمكتب خلال الفترة.", UnitAr = "دينار ليبي", DataSourceAr = "القسم المالي", ObjectiveAr = "قياس العائد المالي للدولة من نشاط الوكالات التجارية.", PublicationFrequency = PublicationFrequency.Semi_Annual },
        new() { Code = "CA-08", NameAr = "متوسط زمن معالجة طلبات الوكالات التجارية", DefinitionAr = "متوسط الوقت المستغرق من تاريخ استلام الطلب المكتمل حتى تاريخ صدور القرار النهائي (موافقة أو رفض).", CalculationMethodAr = "مجموع الأيام من تاريخ تقديم الطلب (المستوفي) إلى تاريخ صدور القرار لجميع الطلبات / إجمالي عدد الطلبات.", UnitAr = "يوم", DataSourceAr = "مكتب الوكالات التجارية", ObjectiveAr = "قياس كفاءة الإجراءات وسرعة إنجاز معاملات المراجعين.", PublicationFrequency = PublicationFrequency.Semi_Annual },
        new() { Code = "CA-09", NameAr = "عدد الطلبات المرفوضة أو المؤجلة", DefinitionAr = "عدد طلبات الوكالات التجارية التي صدرت لها قرارات بالرفض (لعدم استيفاء الشروط) أو تم تأجيلها خلال الفترة.", CalculationMethodAr = "حصر عدد الطلبات التي قوبلت بالرفض أو التأجيل (مع ذكر الأسباب) خلال الفترة.", UnitAr = "طلب", DataSourceAr = "مكتب الوكالات التجارية", ObjectiveAr = "قياس مدى مطابقة الطلبات للشروط والضوابط وتحديد المشكلات.", PublicationFrequency = PublicationFrequency.Semi_Annual },
        new() { Code = "CA-10", NameAr = "عدد الوكلاء التجاريين المقيدين في السجل الخاص", DefinitionAr = "إجمالي عدد الجهات (أفراد أو شركات) المزاولة لنشاط الوكالات التجارية والمسجلة في السجل الخاص لدى المكتب.", CalculationMethodAr = "العدد التراكمي للوكلاء النشطين (الجهات الحاصلة على وكالات سارية) في نهاية الفترة.", UnitAr = "وكيل / جهة", DataSourceAr = "السجل الخاص للوكلاء التجاريين", ObjectiveAr = "حجم قاعدة الوكلاء التجاريين النشطين في السوق.", PublicationFrequency = PublicationFrequency.Annual },
    ];

    // ═══════════════════════════════════════════════════════════════
    // 7. مكتب العلامات التجارية — O-01 to O-09 (9 indicators)
    // ═══════════════════════════════════════════════════════════════
    private static List<Indicator> GetTrademarksIndicators() =>
    [
        new() { Code = "O-01", NameAr = "عدد طلبات قيد العلامات التجارية", DefinitionAr = "إجمالي عدد طلبات تسجيل العلامات التجارية الجديدة المستلمة خلال الفترة (قبل الفحص).", CalculationMethodAr = "حصر عدد طلبات التسجيل الواردة إلى المكتب (سواء ورقياً أو إلكترونياً) خلال الفترة المحددة.", UnitAr = "طلب", DataSourceAr = "قسم العلامات التجارية", ObjectiveAr = "قياس حجم النشاط التجاري والابتكاري في السوق والطلب على حماية الملكية الفكرية.", PublicationFrequency = PublicationFrequency.Quarterly },
        new() { Code = "O-02", NameAr = "عدد قرارات الموافقة على طلبات العلامات", DefinitionAr = "عدد طلبات تسجيل العلامات التجارية التي تم فحصها والموافقة عليها من قبل المكتب خلال الفترة.", CalculationMethodAr = "حصر عدد الطلبات التي صدرت لها قرارات بالموافقة (قبل مرحلة الإشهار) خلال الفترة المحددة.", UnitAr = "قرار", DataSourceAr = "فحص العلامات", ObjectiveAr = "قياس كفاءة وفاعلية إجراءات الفحص والمعالجة.", PublicationFrequency = PublicationFrequency.Quarterly },
        new() { Code = "O-03", NameAr = "عدد العلامات التي أُشهرت بعد حصولها على قرار الموافقة", DefinitionAr = "عدد العلامات التجارية التي أكملت إجراءات النشر في النشرة الرسمية (مرحلة الإشهار) بعد الموافقة عليها.", CalculationMethodAr = "حصر عدد العلامات التي تم نشرها في النشرة الرسمية خلال الفترة.", UnitAr = "علامة", DataSourceAr = "قسم العلامات", ObjectiveAr = "قياس مدى التقدم في إجراءات الإشهار واستكمال الحماية القانونية للعلامات.", PublicationFrequency = PublicationFrequency.Quarterly },
        new() { Code = "O-04", NameAr = "عدد العلامات التجارية المحلية المُشهرة", DefinitionAr = "عدد العلامات التجارية المملوكة لأفراد أو شركات ليبية (وطنية) والتي تم إشهارها خلال الفترة.", CalculationMethodAr = "حصر عدد العلامات المُشهرة (من O-03) التي يقع مالكها في ليبيا (أو تحمل جنسية ليبية).", UnitAr = "علامة محلية", DataSourceAr = "قسم العلامات", ObjectiveAr = "قياس نشاط وحجم العلامات التجارية الوطنية وحماية المنتج المحلي.", PublicationFrequency = PublicationFrequency.Semi_Annual },
        new() { Code = "O-05", NameAr = "عدد العلامات التجارية الأجنبية المُشهرة", DefinitionAr = "عدد العلامات التجارية المملوكة لأفراد أو شركات أجنبية (غير ليبية) والتي تم إشهارها خلال الفترة.", CalculationMethodAr = "حصر عدد العلامات المُشهرة (من O-03) التي يقع مالكها خارج ليبيا (أو تحمل جنسية أجنبية).", UnitAr = "علامة أجنبية", DataSourceAr = "قسم العلامات", ObjectiveAr = "قياس مدى جاذبية السوق الليبي للعلامات التجارية الدولية والانفتاح التجاري.", PublicationFrequency = PublicationFrequency.Semi_Annual },
        new() { Code = "O-06", NameAr = "عدد وكلاء العلامات التجارية المقيدين لدى المكتب", DefinitionAr = "إجمالي عدد مكاتب المحاماة أو الوكلاء المعتمدين (أفراد أو شركات) المرخص لهم قانوناً بتقديم خدمات تسجيل العلامات التجارية نيابة عن الغير.", CalculationMethodAr = "العدد التراكمي للوكلاء النشطين (ذوي القيد الساري) في سجل الوكلاء في نهاية الفترة.", UnitAr = "وكيل / مكتب", DataSourceAr = "سجل وكلاء العلامات التجارية", ObjectiveAr = "تنظيم مهنة وكالة العلامات التجارية وضمان جودة الخدمات المقدمة للمستثمرين.", PublicationFrequency = PublicationFrequency.Annual },
        new() { Code = "O-07", NameAr = "إجمالي الإيرادات المحصلة من نشاط قيد وإشهار العلامات التجارية", DefinitionAr = "القيمة المالية الإجمالية المحصلة نظير جميع الخدمات (رسوم طلب القيد، الفحص، الإشهار، التجديد، التعديل، وغيرها).", CalculationMethodAr = "مجموع المبالغ المالية المودعة في الخزينة العامة أو الحسابات المصرفية للمكتب خلال الفترة.", UnitAr = "دينار ليبي", DataSourceAr = "القسم المالي", ObjectiveAr = "قياس العائد المالي للدولة من خدمات الملكية الفك��ية وتعزيز استدامة المكتب.", PublicationFrequency = PublicationFrequency.Semi_Annual },
        new() { Code = "O-08", NameAr = "عدد التظلمات والاعتراضات على الإشهارات", DefinitionAr = "عدد الاعتراضات الرسمية المقدمة من الغير على العلامات التجارية التي تم نشرها (إشهارها) خلال فترة السماح القانونية.", CalculationMethodAr = "حصر عدد طلبات التظلم أو الاعتراض الواردة إلى لجنة التظلمات خلال الفترة.", UnitAr = "تظلم / اعتراض", DataSourceAr = "لجنة التظلمات / المكتب القانوني", ObjectiveAr = "قياس مستوى التنافسية والنزاعات في السوق، ومدى وعي المتعاملين بحقوقهم.", PublicationFrequency = PublicationFrequency.Semi_Annual },
        new() { Code = "O-09", NameAr = "عدد الأحكام القضائية النهائية ضد تسجيل العلامات التجارية", DefinitionAr = "عدد القرارات القضائية النهائية (الباتة) الصادرة عن المحاكم المختصة والتي قضت برفض أو إلغاء أو شطب تسجيل علامة تجارية.", CalculationMethodAr = "حصر عدد الأحكام النهائية الصادرة ضد تسجيل علامات (لمصلحة معترض أو ضد المكتب) خلال الفترة.", UnitAr = "حكم قضائي", DataSourceAr = "إدارة الشؤون القانونية", ObjectiveAr = "قياس مدى سلامة الإجراءات القانونية للمكتب، وتحديد الثغرات في الفحص أو التشريعات.", PublicationFrequency = PublicationFrequency.Annual },
    ];

    // ═══════════════════════════════════════════════════════════════
    // 8. صندوق ضمان الائتمان — C-01 to C-14 (14 indicators)
    // ═══════════════════════════════════════════════════════════════
    private static List<Indicator> GetCreditGuaranteeIndicators() =>
    [
        new() { Code = "C-01", NameAr = "إجمالي عدد الضمانات الممنوحة", DefinitionAr = "إجمالي عدد شهادات الضمان الائتماني الصادرة (لمشروعات محلية + صادرات) خلال الفترة.", CalculationMethodAr = "العد الفعلي لشهادات الضمان المصدرة والمعتمدة.", UnitAr = "ضمان", DataSourceAr = "إدارة الضمانات", ObjectiveAr = "قياس حجم النشاط والتدفق الجديد للضمانات.", PublicationFrequency = PublicationFrequency.Quarterly },
        new() { Code = "C-02", NameAr = "إجمالي قيمة الائتمان المضمون", DefinitionAr = "إجمالي قيمة التمويلات (القروض) التي تم تغطيتها بضمانات الصندوق خلال الفترة.", CalculationMethodAr = "مجموع قيم القروض المضمونة (بجميع أنواعها) خلال الفترة.", UnitAr = "دينار ليبي", DataSourceAr = "إدارة المخاطر", ObjectiveAr = "قياس حجم التمويل المحفز في الاقتصاد بفضل ضمانات الصندوق.", PublicationFrequency = PublicationFrequency.Quarterly },
        new() { Code = "C-03", NameAr = "توزيع الضمانات حسب حجم المشروع", DefinitionAr = "عدد الضمانات الممنوحة مصنفة حسب حجم المشروع (متناهي الصغر، صغرى، متوسطة).", CalculationMethodAr = "(عدد ضمانات المشروعات متناهية الصغر) + (عدد ضمانات المشروعات الصغرى) + (عدد ضمانات المشروعات المتوسطة).", UnitAr = "ضمان (حسب الفئة)", DataSourceAr = "إدارة الضمانات", ObjectiveAr = "قياس مدى وصول الصندوق للفئات المستهدفة ودعم شريحة المشروعات الصغرى والأكثر احتياجاً.", PublicationFrequency = PublicationFrequency.Semi_Annual },
        new() { Code = "C-04", NameAr = "قيمة الضمانات المصدرة للصادرات", DefinitionAr = "إجمالي قيمة ضمانات مخاطر تمويل الصادرات أو المخاطر الناتجة عن عمليات التصدير.", CalculationMethodAr = "مجموع قيم ضمانات الصادرات الممنوحة خلال الفترة.", UnitAr = "دينار ليبي", DataSourceAr = "إدارة الضمانات / إدارة الصادرات", ObjectiveAr = "دعم وتشجيع الصادرات الليبية غير النفطية وتعزيز تنافسيتها.", PublicationFrequency = PublicationFrequency.Semi_Annual },
        new() { Code = "C-05", NameAr = "نسبة التغطية (المخاطر المشتركة)", DefinitionAr = "نسبة مخاطر التمويل التي يتحملها الصندوق (وفقاً للائحة 70% كحد أقصى) مقارنة بإجمالي التمويل.", CalculationMethodAr = "(إجمالي قيمة الالتزامات المحتملة للصندوق / إجمالي قيمة القروض المضمونة) × 100", UnitAr = "نسبة مئوية (%)", DataSourceAr = "إدارة المخاطر", ObjectiveAr = "تحديد مستوى المخاطر التي يتحملها الصندوق وإدارة كفاية رأس المال.", PublicationFrequency = PublicationFrequency.Annual },
        new() { Code = "C-06", NameAr = "نسبة المشروعات المتعثرة (معدل التعثر)", DefinitionAr = "نسبة الضمانات التي تم تفعيلها (تم دفع التعويض عنها) بسبب تعثر المستفيدين عن السداد.", CalculationMethodAr = "(قيمة الضمانات التي تم تفعيلها وتعويضها / إجمالي قيمة الضمانات الممنوحة) × 100", UnitAr = "نسبة مئوية (%)", DataSourceAr = "إدارة المتابعة / إدارة المخاطر", ObjectiveAr = "قياس جودة المحفظة الائتمانية وفعالية إجراءات تقييم المخاطر.", PublicationFrequency = PublicationFrequency.Annual },
        new() { Code = "C-07", NameAr = "إجمالي التعويضات المدفوعة", DefinitionAr = "القيمة الإجمالية للمبالغ التي دفعها الصندوق للمؤسسات المالية تعويضاً عن حالات التعثر.", CalculationMethodAr = "مجموع قيم التعويضات المدفوعة خلال الفترة.", UnitAr = "دينار ليبي", DataSourceAr = "إدارة المالية / إدارة المتابعة", ObjectiveAr = "قياس الأثر المالي المباشر للمخاطر المحققة.", PublicationFrequency = PublicationFrequency.Annual },
        new() { Code = "C-08", NameAr = "نسبة استرداد القروض بعد التعويض", DefinitionAr = "نسبة المبالغ التي تم استردادها لاحقاً (من المستفيدين المتعثرين) بعد دفع التعويض.", CalculationMethodAr = "(قيمة المبالغ المستردة من المتعثرين / إجمالي التعويضات المدفوعة) × 100", UnitAr = "نسبة مئوية (%)", DataSourceAr = "إدارة المتابعة / الشؤون القانونية", ObjectiveAr = "قياس فعالية إجراءات التحصيل والمتابعة القانونية.", PublicationFrequency = PublicationFrequency.Annual },
        new() { Code = "C-09", NameAr = "عدد فرص العمل المدعومة (المستحدثة)", DefinitionAr = "إجمالي عدد الوظائف التي تم خلقها أو الحفاظ عليها بفضل المشروعات المضمونة.", CalculationMethodAr = "حصر إجمالي عدد العاملين في المشروعات المستفيدة من الضمانات (بناءً على بيانات المشروعات).", UnitAr = "وظيفة", DataSourceAr = "إدارة الدراسات / متابعة المشروعات", ObjectiveAr = "قياس الأثر الاجتماعي والاقتصادي للصندوق في خفض البطالة.", PublicationFrequency = PublicationFrequency.Annual },
        new() { Code = "C-10", NameAr = "عدد الاتفاقيات المبرمة مع المؤسسات المالية", DefinitionAr = "عدد اتفاقيات التعاون وضمان الائتمان الموقعة مع المصارف والمؤسسات المالية العاملة.", CalculationMethodAr = "العدد التراكمي للاتفاقيات النشطة والسارية مع الجهات الممولة.", UnitAr = "اتفاقية", DataSourceAr = "إدارة الشؤون القانونية / إدارة العلاقات", ObjectiveAr = "توسيع شبكة الشراكات مع القطاع المصرفي وزيادة الانتشار.", PublicationFrequency = PublicationFrequency.Semi_Annual },
        new() { Code = "C-11", NameAr = "عدد الدراسات والبحوث الصادرة", DefinitionAr = "عدد التقارير والدراسات المتعلقة بسلامة وجودة نظام الائتمان وتحليل المخاطر.", CalculationMethodAr = "حصر عدد الدراسات والتقارير الفنية الصادرة خلال الفترة.", UnitAr = "دراسة / تقرير", DataSourceAr = "إدارة الدراسات والبحوث", ObjectiveAr = "دعم اتخاذ القرار بتحليلات موضوعية وتطوير آليات العمل.", PublicationFrequency = PublicationFrequency.Semi_Annual },
        new() { Code = "C-12", NameAr = "عدد ورش العمل والندوات المنفذة", DefinitionAr = "عدد الفعاليات التوعوية والتدريبية (ندوات، مؤتمرات، ورش عمل) حول ثقافة الائتمان والضمان.", CalculationMethodAr = "حصر عدد الفعاليات المنفذة خلال الفترة.", UnitAr = "فعالية / ورشة", DataSourceAr = "إدارة العلاقات / التدريب", ObjectiveAr = "نشر الوعي بآليات الضمان الائتماني وتحفيز الطلب على التمويل.", PublicationFrequency = PublicationFrequency.Semi_Annual },
        new() { Code = "C-13", NameAr = "متوسط حجم المشروع المضمون", DefinitionAr = "متوسط قيمة التمويل للمشروعات التي حصلت على ضمانات.", CalculationMethodAr = "(إجمالي قيمة القروض المضمونة / إجمالي عدد الضمانات الممنوحة)", UnitAr = "دينار ليبي", DataSourceAr = "إدارة المخاطر", ObjectiveAr = "تحليل طبيعة المشروعات المدعومة وحجم احتياجاتها التمويلية.", PublicationFrequency = PublicationFrequency.Annual },
        new() { Code = "C-14", NameAr = "نسبة تغطية القطاعات الاقتصادية", DefinitionAr = "توزيع الضمانات الممنوحة حسب القطاعات (زراعي، صناعي، خدمي، تجاري).", CalculationMethodAr = "��دد الضمانات لكل قطاع / إجمالي عدد الضمانات × 100", UnitAr = "نسبة مئوية (%)", DataSourceAr = "إدارة الضمانات", ObjectiveAr = "قياس مدى تنوع المحفظة ودعم القطاعات الإنتاجية ذات الأولوية.", PublicationFrequency = PublicationFrequency.Annual },
    ];

    // ═══════════════════════════════════════════════════════════════
    // 9. هيئة الإشراف على التأمين — I-01 to I-05 (5 indicators)
    // ═══════════════════════════════════════════════════════════════
    private static List<Indicator> GetInsuranceIndicators() =>
    [
        new() { Code = "I-01", NameAr = "عدد التراخيص النشطة", DefinitionAr = "شركات التأمين والوسطاء المعتمدين.", CalculationMethodAr = "حصر السجلات.", UnitAr = "عدد", DataSourceAr = "هيئة الإشراف على التأمين", ObjectiveAr = "تنظيم السوق.", PublicationFrequency = PublicationFrequency.Annual },
        new() { Code = "I-02", NameAr = "إجمالي الأقساط المكتتبة", DefinitionAr = "حجم التدفقات المالية في سوق التأمين.", CalculationMethodAr = "مجموع الأقساط.", UnitAr = "دينار", DataSourceAr = "هيئة الإشراف على التأمين", ObjectiveAr = "نمو القطاع.", PublicationFrequency = PublicationFrequency.Annual },
        new() { Code = "I-03", NameAr = "إجمالي التعويضات المدفوعة", DefinitionAr = "المبالغ المسددة للمتضررين.", CalculationMethodAr = "مجموع المطالبات المسددة.", UnitAr = "دينار", DataSourceAr = "الدراسات", ObjectiveAr = "حماية المؤمن له.", PublicationFrequency = PublicationFrequency.Annual },
        new() { Code = "I-04", NameAr = "إجمالي تعويضات تحت التسوية", DefinitionAr = "القيمة الإجمالية للمطالبات التي تم الإبلاغ عنها للشركات ولم يتم صرف قيمتها للمستفيدين بعد، سواء لأنها قيد الدراسة، أو التحقيق، أو لم تكتمل مستنداتها، أو تم رفضها بشكل مبدئي وقيد المراجعة.", CalculationMethodAr = "مجموع أرصدة مطالبات تحت التسوية في القوائم المالية لجميع شركات التأمين العاملة في نهاية الفترة.", UnitAr = "دينار", DataSourceAr = "هيئة الإشراف على التأمين", ObjectiveAr = "قياس الالتزامات المالية المستحقة على الشركات تجاه المؤمن لهم أو المستفيدين، ومراقبة كفاءة الشركات في تسوية المطالبات.", PublicationFrequency = PublicationFrequency.Annual },
        new() { Code = "I-05", NameAr = "مجموع استثمارات الشركات", DefinitionAr = "حجم الأموال المستثمرة من قبل شركات التأمين.", CalculationMethodAr = "حجم الاستثمارات.", UnitAr = "دينار", DataSourceAr = "هيئة الإشراف على التأمين", ObjectiveAr = "دعم الاقتصاد.", PublicationFrequency = PublicationFrequency.Annual },
    ];

    // ═══════════════════════════════════════════════════════════════
    // 10. الهيئة العامة للمعارض — Exh.01 to Exh.06 (6 indicators)
    // ═══════════════════════════════════════════════════════════════
    private static List<Indicator> GetExhibitionsIndicators() =>
    [
        new() { Code = "Exh-01", NameAr = "عدد المعارض المقامة محلياً", DefinitionAr = "حجم النشاط في قطاع صناعة المعارض داخل ليبيا.", CalculationMethodAr = "العدد المباشر للمعارض (العامة والتخصصية) التي تم تنفيذها فعلياً خلال العام، سواء من قبل الهيئة أو القطاع الخاص.", UnitAr = "معرض", DataSourceAr = "إدارة المعارض / إدارة التراخيص", ObjectiveAr = "قياس حجم النشاط الاقتصادي للمعارض وتطور قطاع تنظيم الفعاليات.", PublicationFrequency = PublicationFrequency.Annual },
        new() { Code = "Exh-02", NameAr = "عدد الشركات الليبية المشاركة في معارض محلية", DefinitionAr = "مدى وصول المنتج الليبي للأسواق المحلية ومدى تفاعل القطاع الخاص مع فعاليات المعارض.", CalculationMethodAr = "إجمالي عدد الشركات الوطنية العارضة (وليس الزائرة) في جميع المعارض المحلية التي أقيمت خلال الفترة.", UnitAr = "شركة", DataSourceAr = "قسم المشاركات المحلية (من سجلات المشاركين)", ObjectiveAr = "قياس مدى إقبال القطاع الخاص الليبي على الترويج لمنتجاته محلياً.", PublicationFrequency = PublicationFrequency.Quarterly },
        new() { Code = "Exh-03", NameAr = "عدد الشركات الأجنبية المشاركة في معارض محلية", DefinitionAr = "مدى جاذبية السوق الليبي للاستثمار والمنتجات الأجنبية، ومدى انفتاح الاقتصاد المحلي.", CalculationMethodAr = "إجمالي عدد الشركات الأجنبية (غير الليبية) التي شاركت كعارضين في المعارض المحلية خلال الفترة.", UnitAr = "شركة", DataSourceAr = "قسم المشاركات الخارجية (من سجلات العارضين الدوليين)", ObjectiveAr = "قياس مدى نجاح الهيئة في جذب شركات أجنبية وإبراز فرص الاستثمار والتبادل التجاري.", PublicationFrequency = PublicationFrequency.Quarterly },
        new() { Code = "Exh-04", NameAr = "عدد المعارض الخارجية التي شاركت فيها الهيئة", DefinitionAr = "تواجد المنتج الليبي في الأسواق الدولية ومدى فاعلية الترويج للصادرات الوطنية.", CalculationMethodAr = "إجمالي عدد المحافل الدولية (معارض، مؤتمرات، تظاهرات اقتصادية) التي مثلت ليبيا فيها الهيئة (بجناح وطني أو وفد رسمي).", UnitAr = "معرض / مشاركة", DataSourceAr = "إدارة العلاقات الدولية / إدارة المعارض الخارجية", ObjectiveAr = "قياس حجم الجهود المبذولة لفتح أسواق جديدة للمنتج الليبي وتعزيز التبادل التجاري.", PublicationFrequency = PublicationFrequency.Annual },
        new() { Code = "Exh-05", NameAr = "إجمالي عوائد المعارض", DefinitionAr = "الإيرادات المالية المحققة للهيئة أو الدولة من نشاط المعارض.", CalculationMethodAr = "مجموع الإيرادات المحصلة (رسوم المشاركة، إيجار المساحات والأجنحة، رسوم الترخيص للمنظمين، إيرادات الرعاية، الخدمات المقدمة).", UnitAr = "دينار ليبي", DataSourceAr = "الإدارة المالية (الحسابات الختامية)", ObjectiveAr = "قياس مساهمة قطاع المعارض في الإيرادات العامة، وتعزيز استدامة الهيئة مالياً.", PublicationFrequency = PublicationFrequency.Annual },
        new() { Code = "Exh-06", NameAr = "عدد الشركات المزاولة لنشاط تنظيم المعارض (المقيدة)", DefinitionAr = "حجم سوق تنظيم المعارض المنظم ومدى احترافية القطاع الخاص في هذا المجال.", CalculationMethodAr = "عدد الشركات والكيانات التي استوفت الشروط وحصلت على ترخيص/قيد ساري المفعول لمزاولة مهنة تنظيم وإدارة المعارض والمؤتمرات.", UnitAr = "شركة", DataSourceAr = "إدارة التراخيص (سجل منظمي المعارض)", ObjectiveAr = "تنظيم السوق، وضمان احترافية مقدمي الخدمات، وتفعيل دور القطاع الخاص وفق أطر قانونية.", PublicationFrequency = PublicationFrequency.Annual },
    ];

    // ═══════════════════════════════════════════════════════════════
    // 11. هيئة تنمية الصادرات الليبية — X-01 to X-12 (12 indicators)
    // ═══════════════════════════════════════════════════════════════
    private static List<Indicator> GetExportDevelopmentIndicators() =>
    [
        new() { Code = "X-01", NameAr = "إجمالي قيمة الصادرات غير النفطية", DefinitionAr = "القيمة الإجمالية للصادرات الليبية من السلع والمنتجات غير النفطية (زراعية، صناعية، حرفية) المسجلة رسمياً.", CalculationMethodAr = "مجموع قيم الصادرات غير النفطية (وفقاً لبيانات الجمارك وسجلات الهيئة) خلال الفترة.", UnitAr = "مليون دينار / دولار", DataSourceAr = "إدارة المعلومات / مصلحة الجمارك (بالتنسيق)", ObjectiveAr = "قياس مساهمة القطاعات غير النفطية في الدخل القومي وتنويع مصادر الدخل.", PublicationFrequency = PublicationFrequency.Quarterly },
        new() { Code = "X-02", NameAr = "عدد المصدرين النشطين المقيدين بسجل الهيئة", DefinitionAr = "عدد الشركات والمنشآت الفردية الوطنية المسجلة في سجل المصدرين والتي لديها صادرات فعلية خلال الفترة.", CalculationMethodAr = "حصر عدد المصدرين الذين قاموا بعمليات تصدير فعلية (وليس فقط المسجلين) خلال الفترة.", UnitAr = "مصدر / شركة", DataSourceAr = "إدارة الدعم ومساندة الصادرات (سجل المصدرين)", ObjectiveAr = "قياس حجم القاعدة التصديرية النشطة وتحفيز انخراط القطاع الخاص في التصدير.", PublicationFrequency = PublicationFrequency.Semi_Annual },
        new() { Code = "X-03", NameAr = "عدد الأسواق الخارجية المخترقة", DefinitionAr = "عدد الدول التي تم التصدير إليها فعلياً خلال الفترة (الأسواق التي وصلتها المنتجات الليبية).", CalculationMethodAr = "حصر الدول المستوردة للمنتجات الليبية غير النفطية (حسب بيانات الشحن والجمارك).", UnitAr = "دولة / سوق", DataSourceAr = "إدارة المعلومات / مصلحة الجمارك", ObjectiveAr = "قياس مدى الانتشار الجغرافي للصادرات الليبية وتنويع الأسواق.", PublicationFrequency = PublicationFrequency.Annual },
        new() { Code = "X-04", NameAr = "عدد مكاتب الشباك الموحد للتصدير العاملة في المنافذ", DefinitionAr = "عدد النقاط الحدودية (مطارات، موانئ، منافذ برية) التي تم تفعيل وتشغيل مكتب للشباك الموحد لخدمة المصدرين.", CalculationMethodAr = "العدد التراكمي للمكاتب العاملة والمرتبطة إلكترونياً بالهيئة.", UnitAr = "مكتب", DataSourceAr = "إدارة العمليات / الإدارة العامة", ObjectiveAr = "تسهيل إجراءات التصدير وتقليل الوقت والجهد على المصدرين.", PublicationFrequency = PublicationFrequency.Semi_Annual },
        new() { Code = "X-05", NameAr = "عدد مراكز دعم المصدرين المنشأة والمنتشرة", DefinitionAr = "عدد المراكز الخدمية المتخصصة التي تم إنشاؤها في البلديات المنتجة (غريان، ترهونة، الجفرة، وغيرها) لدعم سلاسل الإمداد.", CalculationMethodAr = "العدد التراكمي للمراكز التي تم تدشينها وبدأت في تقديم خدماتها الفعلية.", UnitAr = "مركز", DataSourceAr = "إدارة الدعم ومساندة الصادرات", ObjectiveAr = "توسيع نطاق الخدمات التصديرية لتصل إلى مناطق الإنتاج وتقليل التكاليف اللوجستية.", PublicationFrequency = PublicationFrequency.Annual },
        new() { Code = "X-06", NameAr = "عدد المستفيدين من خدمات مراكز دعم المصدرين", DefinitionAr = "عدد المصدرين والمنتجين الذين استفادوا من الخدمات المقدمة في المراكز (تعبئة، تبريد، مختبرات، استشارات).", CalculationMethodAr = "إجمالي عدد المستفيدين (شركات وأفراد) من خدمات المراكز خلال الفترة.", UnitAr = "مستفيد", DataSourceAr = "مراكز دعم المصدرين (سجل المستفيدين)", ObjectiveAr = "قياس مدى إقبال القطاع الخاص على الخدمات المقدمة وفعالية المراكز.", PublicationFrequency = PublicationFrequency.Quarterly },
        new() { Code = "X-07", NameAr = "عدد الخدمات المقدمة عبر بوابة المصدر الإلكترونية", DefinitionAr = "عدد الخدمات الإلكترونية المتاحة للمصدرين عبر البوابة (تسجيل، استعلام، متابعة معاملات، إصدار تراخيص).", CalculationMethodAr = "العدد التراكمي للخدمات التي تم رقمنتها وتفعيلها على المنصة.", UnitAr = "خدمة", DataSourceAr = "إدارة تقنية المعلومات / بوابة المصدر", ObjectiveAr = "قياس التقدم في التحول الرقمي وتسهيل وصول المصدرين للخدمات.", PublicationFrequency = PublicationFrequency.Semi_Annual },
        new() { Code = "X-08", NameAr = "عدد الدراسات والبحوث الصادرة", DefinitionAr = "عدد التقارير والدراسات والتحليلات التي أعدتها الهيئة حول الفرص التصديرية، الأسواق المستهدفة، والميز التنافسية للمنتجات الليبية.", CalculationMethodAr = "حصر عدد الدراسات والأوراق البحثية المنشورة (أو المرفوعة) خلال الفترة.", UnitAr = "دراسة / تقرير", DataSourceAr = "قسم الدراسات والبحوث", ObjectiveAr = "دعم المصدرين بالمعلومات والتحليلات اللازمة لاتخاذ قرارات تصديرية سليمة.", PublicationFrequency = PublicationFrequency.Semi_Annual },
        new() { Code = "X-09", NameAr = "عدد البرامج التدريبية والتأهيلية المنفذة للمصدرين", DefinitionAr = "عدد الدورات وورش العمل والندوات التي نفذتها الهيئة لتأهيل المصدرين في مجالات التصدير، التعبئة، المواصفات العالمية، والتسويق الدولي.", CalculationMethodAr = "حصر عدد البرامج التدريبية المنفذة خلال الفترة.", UnitAr = "برنامج / دورة", DataSourceAr = "إدارة الدعم ومساندة الصادرات", ObjectiveAr = "رفع كفاءة المصدرين وتأهيلهم للتعامل مع الأسواق العالمية.", PublicationFrequency = PublicationFrequency.Semi_Annual },
        new() { Code = "X-10", NameAr = "عدد المشاركات الدولية في المعارض والبعثات الترويجية", DefinitionAr = "عدد المعارض الدولية والمؤتمرات والبعثات الترويجية التي شاركت فيها الهيئة (بجناح وطني أو وفد رسمي).", CalculationMethodAr = "حصر عدد المشاركات الخارجية التي تم تنظيمها خلال الفترة.", UnitAr = "مشاركة", DataSourceAr = "إدارة الترويج / الإدارة العامة", ObjectiveAr = "الترويج للمنتج الليبي في الخارج وفتح قنوات تسويقية جديدة.", PublicationFrequency = PublicationFrequency.Annual },
        new() { Code = "X-11", NameAr = "عدد المنتجات الليبية المسجلة أو المؤهلة للتصدير", DefinitionAr = "عدد المنتجات الوطنية (زراعية، صناعية) التي تم تأهيلها وتتوافق مع المواصفات القياسية الدولية للتصدير.", CalculationMethodAr = "حصر عدد المنتجات التي حصلت على شهادات مطابقة أو تم تسجيلها كمنتجات قابلة للتصدير.", UnitAr = "منتج", DataSourceAr = "إدارة الدعم ومساندة الصادرات / مراكز الدعم", ObjectiveAr = "رفع جودة المنتجات الوطنية لتلبية متطلبات الأسواق الخارجية.", PublicationFrequency = PublicationFrequency.Annual },
        new() { Code = "X-12", NameAr = "حجم التفاعل الرقمي على منصات التواصل الاجتماعي", DefinitionAr = "مدى الوصول والتفاعل مع محتوى الهيئة على وسائل التواصل (فيسبوك، تويتر، إنستغرام) كمؤشر على الوعي والانتشار.", CalculationMethodAr = "إجمالي عدد المتابعين، التفاعلات (إعجاب، تعليق، مشاركة)، ومدى الوصول للمنشورات.", UnitAr = "تفاعل / متابع", DataSourceAr = "صفحة الفيسبوك / إدارة الإعلام", ObjectiveAr = "قياس مدى فعالية التواصل الرقمي مع المصدرين والمهتمين ونشر الوعي التصديري.", PublicationFrequency = PublicationFrequency.Monthly },
    ];

    // ═══════════════════════════════════════════════════════════════
    // 12. شبكة ليبيا للتجارة — LTN.01 to LTN.06 (6 indicators)
    // ═══════════════════════════════════════════════════════════════
    private static List<Indicator> GetTradeNetworkIndicators() =>
    [
        new() { Code = "LTN-01", NameAr = "عدد الجهات المرتبطة إلكترونياً بالشبكة", DefinitionAr = "عدد الجهات الحكومية والخدمية (جمارك، مصارف، موانئ، وزارات، هيئات) التي تم ربطها فعلياً بمنظومة النافذة الواحدة وتبادل البيانات إلكترونياً.", CalculationMethodAr = "العدد التراكمي للجهات التي اكتمل ربطها التقني وبدأت في تبادل المعاملات عبر الشبكة.", UnitAr = "جهة", DataSourceAr = "إدارة التشغيل", ObjectiveAr = "قياس مدى تحقيق التكامل التقني والمعلوماتي بين المؤسسات العاملة في مجال التجارة.", PublicationFrequency = PublicationFrequency.Annual },
        new() { Code = "LTN-02", NameAr = "عدد المتاجر الإلكترونية المسجلة", DefinitionAr = "حجم سوق التجارة الإلكترونية المنظم والمرخص الذي يخضع لمتابعة الشبكة.", CalculationMethodAr = "العدد التراكمي للمتاجر والمنصات الإلكترونية التي أتمت عملية التسجيل بنجاح على منصة الشبكة وحصلت على ترخيص ساري المفعول.", UnitAr = "متجر", DataSourceAr = "منصة تسجيل وتنظيم المتاجر الإلكترونية", ObjectiveAr = "قياس مدى تنظيم السوق الرقمي وحصر النشاط التجاري الإلكتروني ودمجه في الاقتصاد الرسمي.", PublicationFrequency = PublicationFrequency.Quarterly },
        new() { Code = "LTN-03", NameAr = "متوسط زمن معالجة المعاملات عبر الشبكة", DefinitionAr = "مؤشر كفاءة أداء المنظومة الإلكترونية في تسهيل وتسيير حركة التجارة مقارنة بالوضع اليدوي السابق.", CalculationMethodAr = "(متوسط الوقت المستغرق لإتمام معاملة تجارية نموذجية عبر الشبكة حالياً / متوسط الوقت المستغرق لإتمام نفس المعاملة قبل الأتمتة) × 100", UnitAr = "نسبة تحسن (%) أو (ساعة/يوم)", DataSourceAr = "إدارة تيسير وتطوير التجارة", ObjectiveAr = "قياس فعالية التحول الرقمي في تسهيل التجارة وتقليل الوقت والجهد.", PublicationFrequency = PublicationFrequency.Semi_Annual },
        new() { Code = "LTN-04", NameAr = "إجمالي عدد المستفيدين من برامج التدريب", DefinitionAr = "حجم الأنشطة التدريبية التي تنفذها الشبكة لتنمية وتأهيل الكوادر الوطنية في مجالات التجارة الإلكترونية والتحول الرقمي والتجارة الخارجية.", CalculationMethodAr = "إجمالي عدد الأفراد (موظفين في جهات حكومية، قطاع خاص، أفراد) الذين شاركوا في الدورات والبرامج التدريبية التي نظمها معهد التدريب أو إدارة تنمية القدرات خلال الفترة.", UnitAr = "مستفيد / متدرب", DataSourceAr = "معهد التدريب التابع للشبكة", ObjectiveAr = "قياس مدى مساهمة الشبكة في تدريب وتنمية القدرات المحلية وفقاً للاختصاصات.", PublicationFrequency = PublicationFrequency.Semi_Annual },
        new() { Code = "LTN-05", NameAr = "نسبة اجتياز برامج التدريب", DefinitionAr = "مدى فعالية وجودة البرامج التدريبية المقدمة وقدرتها على تحقيق أهدافها في رفع كفاءة المتدربين.", CalculationMethodAr = "(عدد المشاركين الذين اجتازوا الدورات بنجاح (وحصلوا على شهادة) / إجمالي عدد المشاركين في الدورات) × 100", UnitAr = "نسبة (%)", DataSourceAr = "معهد التدريب التابع للشبكة", ObjectiveAr = "قياس فعالية وكفاءة برامج إعداد الموارد البشرية وتأهيلها.", PublicationFrequency = PublicationFrequency.Semi_Annual },
        new() { Code = "LTN-06", NameAr = "كفاءة نقطة الاستعلام", DefinitionAr = "فعالية نقطة الاستعلام في تلبية احتياجات المتعاملين في التجارة الخارجية من خلال الرد على استفساراتهم وشكاواهم وتظلماتهم.", CalculationMethodAr = "(عدد الردود المقدمة على الاستفسارات والشكاوى والتظلمات / إجمالي عدد الاستفسارات والشكاوى والتظلمات الواردة) × 100", UnitAr = "نسبة (%)", DataSourceAr = "لجنة إدارة وتشغيل نقطة الاستعلام", ObjectiveAr = "قياس مدى تلبية نقطة الاستعلام لا��تياجات المستفيدين وفعالية التواصل معهم.", PublicationFrequency = PublicationFrequency.Quarterly },
    ];

    // ═══════════════════════════════════════════════════════════════
    // 13. الهيئة العامة لتشجيع الاستثمار — V-01 to V-06 (6 indicators)
    // ═══════════════════════════════════════════════════════════════
    private static List<Indicator> GetInvestmentIndicators() =>
    [
        new() { Code = "V-01", NameAr = "عدد المشاريع الاستثمارية المعتمدة", DefinitionAr = "المشاريع الاستثمارية (محلية وأجنبية) التي حصلت على موافقات الهيئة وتم إصدار تراخيص مزاولة النشاط لها خلال الفترة.", CalculationMethodAr = "حصر عدد التراخيص الجديدة الصادرة (شهادات الإيداع، عقود الانتفاع، موافقات التأسيس) خلال الفترة المحددة.", UnitAr = "مشروع / ترخيص", DataSourceAr = "الهيئة العامة لتشجيع الاستثمار وشؤون الخصخصة", ObjectiveAr = "قياس حجم التدفق الجديد للمشاريع الاستثمارية الوافدة إلى السوق الليبي.", PublicationFrequency = PublicationFrequency.Semi_Annual },
        new() { Code = "V-02", NameAr = "رأس المال المستثمر", DefinitionAr = "إجمالي قيمة رؤوس الأموال المقررة (المعلنة) للمشاريع الاستثمارية التي تم اعتمادها خلال الفترة.", CalculationMethodAr = "مجموع رؤوس أموال المشاريع الاستثمارية الجديدة والمعتمدة (وفقاً لعقود التأسيس أو التراخيص) خلال الفترة.", UnitAr = "دينار", DataSourceAr = "الهيئة العامة لتشجيع الاستثمار وشؤون الخصخصة", ObjectiveAr = "تحسين المناخ الاستثماري وقياس حجم رأس المال الوطني والأجنبي المستقطب.", PublicationFrequency = PublicationFrequency.Semi_Annual },
        new() { Code = "V-03", NameAr = "فرص العمل المستحدثة (المخطط لها)", DefinitionAr = "إجمالي عدد الوظائف التي تعهد المستثمرون بتوفيرها في مشاريعهم المعتمدة، كمساهمة متوقعة في سوق العمل.", CalculationMethodAr = "حصر إجمالي عدد فرص العمل المعلنة (الليبية والأجنبية) في دراسات الجدوى أو عقود المشاريع الاستثمارية المعتمدة خلال الفترة.", UnitAr = "وظيفة (مخطط لها)", DataSourceAr = "الهيئة العامة لتشجيع الاستثمار وشؤون الخصخصة", ObjectiveAr = "المساهمة في خفض البطالة وقياس الأثر الاجتماعي والاقتصادي للمشاريع.", PublicationFrequency = PublicationFrequency.Annual },
        new() { Code = "V-04", NameAr = "حجم الاستثمار الأجنبي المباشر (FDI) المستقطب", DefinitionAr = "قيمة رؤوس الأموال الأجنبية (غير الليبية) التي دخلت الاقتصاد الوطني عبر مشاريع استثمارية جديدة أو توسعات معتمدة خلال الفترة.", CalculationMethodAr = "مجموع قيم حصص الشركاء الأجانب في رؤوس أموال المشاريع الاستثمارية المرخصة حديثاً + أي توسعات برؤوس أموال أجنبية لمشاريع قائمة.", UnitAr = "مليون دينار", DataSourceAr = "الهيئة العامة لتشجيع الاستثمار وشؤون الخصخصة", ObjectiveAr = "قياس مدى نجاح الهيئة في جذب رؤوس الأموال الأجنبية وتوطينها.", PublicationFrequency = PublicationFrequency.Annual },
        new() { Code = "V-05", NameAr = "نسبة المشاريع الاستثمارية التي دخلت مرحلة التشغيل", DefinitionAr = "مدى نجاح المشاريع المرخصة في الانتقال من مرحلة التخطيط والترخيص إلى مرحلة الإنتاج الفعلي للسلع أو الخدمات.", CalculationMethodAr = "(عدد المشاريع التي بدأت التشغيل الفعلي / إجمالي عدد المشاريع النشطة والمرخصة في بداية الفترة) × 100", UnitAr = "نسبة مئوية (%)", DataSourceAr = "إدارة المتابعة", ObjectiveAr = "قياس فعالية المتابعة وتحويل الموافقات إلى قيمة اقتصادية حقيقية على أرض الواقع.", PublicationFrequency = PublicationFrequency.Semi_Annual },
        new() { Code = "V-06", NameAr = "معدل نمو الاستثمار المحلي", DefinitionAr = "التغير السنوي في حجم استثمارات القطاع الخاص المحلي (الأفراد والشركات الليبية) المعتمدة من الهيئة.", CalculationMethodAr = "((إجمالي قيمة رؤوس الأموال المحلية المستثمرة في السنة الحالية - إجمالي قيمة رؤوس الأموال المحلية المستثمرة في السنة السابقة) / إجمالي قيمة رؤوس الأموال المحلية المستثمرة في السنة السابقة) × 100", UnitAr = "نسبة مئوية (%)", DataSourceAr = "الهيئة العامة لتشجيع الاستثمار وشؤون الخصخصة", ObjectiveAr = "قياس مدى تحسن ثقة رأس المال المحلي في بيئة الاستثمار الوطنية.", PublicationFrequency = PublicationFrequency.Annual },
    ];

    // ═══════════════════════════════════════════════════════════════
    // 14. هيئة سوق المال الليبي — Mkt.01 to Mkt.05 (5 indicators)
    // ═══════════════════════════════════════════════════════════════
    private static List<Indicator> GetCapitalMarketIndicators() =>
    [
        new() { Code = "Mkt-01", NameAr = "رأس المال السوقي للشركات المدرجة", DefinitionAr = "القيمة السوقية الإجمالية لجميع الشركات المدرجة في السوق.", CalculationMethodAr = "مجموع (عدد الأسهم المصدرة والمتداولة لكل شركة مدرجة × سعر إغلاق السهم في نهاية الفترة)", UnitAr = "دينار ليبي", DataSourceAr = "إدارة التداول", ObjectiveAr = "قياس حجم السوق ونموه وجاذبيته للاستثمار.", PublicationFrequency = PublicationFrequency.Annual },
        new() { Code = "Mkt-02", NameAr = "حجم التداول السنوي", DefinitionAr = "قيمة الأسهم والأوراق المالية التي تم تداولها (بيع/شراء) خلال السنة.", CalculationMethodAr = "مجموع قيم جميع صفقات البيع والشراء المنفذة على جميع الأوراق المالية خلال الفترة.", UnitAr = "دينار ليبي", DataSourceAr = "إدارة التداول", ObjectiveAr = "قياس مستوى السيولة والنشاط في السوق ومدى حيويته.", PublicationFrequency = PublicationFrequency.Quarterly },
        new() { Code = "Mkt-03", NameAr = "عدد شركات الوساطة المرخصة", DefinitionAr = "حجم البنية التحتية للخدمات المالية في السوق ومدى انتشارها.", CalculationMethodAr = "العدد الإجمالي لشركات الوساطة المالية الحاصلة على ترخيص ساري المفعول من الهيئة.", UnitAr = "شركة", DataSourceAr = "إدارة التراخيص (سجل الوسطاء)", ObjectiveAr = "قياس مدى تطور قطاع الخدمات المالية المساعدة وانتشاره.", PublicationFrequency = PublicationFrequency.Semi_Annual },
        new() { Code = "Mkt-04", NameAr = "مكاتب المحاسبة والمراجعة القانونية المعتمدة لدى الهيئة", DefinitionAr = "عدد مكاتب المحاسبة والمراجعة المؤهلة والمرخص لها بمراجعة القوائم المالية للجهات الخاضعة لرقابة الهيئة (شركات مدرجة، شركات وساطة، صناديق استثمار).", CalculationMethodAr = "العدد الإجمالي للمكاتب المهنية (أو الشركات) التي استوفت شروط الهيئة وتم اعتمادها لمزاولة مهام المراجعة القانونية لدى الجهات الخاضعة للإشراف.", UnitAr = "مكتب / شركة", DataSourceAr = "إدارة التراخيص / إدارة الإشراف (سجل مراقبي الحسابات)", ObjectiveAr = "ضمان جودة المعلومات المالية المقدمة للمستثمرين من خلال مراجعين مؤهلين.", PublicationFrequency = PublicationFrequency.Annual },
        new() { Code = "Mkt-05", NameAr = "مكاتب الاستشارات المعتمدة لدى الهيئة", DefinitionAr = "عدد الشركات والمكاتب المؤهلة والمرخص لها بتقديم خدمات استشارية مالية (استشارات استثمارية، دراسات جدوى، تقييم منشآت، ترتيب إصدارات) للجهات الراغبة في الدخول إلى السوق.", CalculationMethodAr = "العدد الإجمالي لشركات الاستشارات المالية الحاصلة على ترخيص ساري المفعول من الهيئة لمزاولة نشاطها.", UnitAr = "مكتب / شركة", DataSourceAr = "إدارة التراخيص (سجل المستشارين الماليين)", ObjectiveAr = "تنظيم الخدمات الاستشارية المالية وضمان احترافية الطروحات الجديدة والإفصاحات.", PublicationFrequency = PublicationFrequency.Annual },
    ];
}
