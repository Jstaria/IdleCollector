using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace IdleEngine
{
    public static class WebHelper
    {
        public static async Task<Texture2D> RetrieveImage(GraphicsDevice _graphics, string imageURL)
        {
            using (MemoryStream stream = new MemoryStream(await GetImageBytes(imageURL)))
            {
                return Texture2D.FromStream(_graphics, stream);
            }
        }

        public static async Task<byte[]> GetImageBytes(string imageURL)
        {
            using (HttpClient client = new HttpClient())
            {
                return await client.GetByteArrayAsync(imageURL);
            }
        }
    }
}
