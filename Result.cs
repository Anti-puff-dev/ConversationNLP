using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLP
{
    public class Result
    {
        public static Models.Intent[] Normalize(Models.Intent[] categories)
        {

            double sum = 0d;

            foreach (Models.Intent category in categories)
            {
                category.confidence = category.weigths_avg * category.relevance_avg;
                sum += category.confidence;
            }

            categories = Confidences(categories, sum);

            return categories;
        }


        public static Models.Intent[] Confidences(Models.Intent[] categories, double sum)
        {
            foreach (Models.Intent category in categories)
            {
                category.confidence = category.confidence / sum;
            }

            return categories;
        }


        public static void Print(Models.Intent[] categories, int? level = null)
        {
            foreach (Models.Intent category in categories)
            {
                if (level != null)
                {
                    for (int i = 0; i < level; i++)
                    {
                        Console.Write(">>");
                    }
                }
                Console.WriteLine(category.name + " " + category.confidence);

                if (category.subcategories.Length > 0)
                {
                    Print(category.subcategories, level + 1);
                }
            }
            Console.WriteLine();
        }
    }
}
