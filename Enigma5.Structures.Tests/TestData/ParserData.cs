/*
    Aenigma - Federal messaging system
    Copyright (C) 2024  Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

    This file is part of Aenigma project.

    Aenigma is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Aenigma is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Aenigma.  If not, see <https://www.gnu.org/licenses/>.
*/

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Enigma5.Crypto.DataProviders;

namespace Enigma5.Structures.Tests.TestData;

[ExcludeFromCodeCoverage]
public class ParserData : IEnumerable<object?[]>
{
    public IEnumerator<object?[]> GetEnumerator()
    {
        yield return new object[] {
            "AUhnrSEaW19iDvnKz6uLWKOt81j0qyBWt7ZYX17jG5kf3eYmcyqlgtEmk5YAqhd5JVWNJNwdqP8FSYuOz36W1KED1VeE4649KRljMsLoEbzEVH1cVryqQGA1Y7kkYKW1vj1IPeAhwch12rX8k8dZAbMDcwkK+8BuzS9CrXajfVUVzE2WpZk//EXV/fduAb7xpq+1UMYO/hRXh7k2M3H+y106Xsre8G9uKJMIlzZj+MRWxS5sbta/LdKwX4Wv+oEa4WpKThcjtqtGF90UulJIvVPbSBWkVHwrafllfNnsVePHneCtJ1t7bjZMtKYDTOtPw/2IVo6iekbsKGKlSVDxQxB4sEIwF6tJH8SWgU0i99p/W3IGx050xiEId/I8lyxUkvDF5JEgOtalPznHtdIQfE4tcd8cKJVikQFBpCiyFcfSQMhDy5+xAgWA",
            PKey.PrivateKey1,
            PKey.Passphrase,
            true,
            PKey.Address1,
            new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x23, 0x56, 0x11 }
        };
        yield return new object?[] {
            "AUhnrSEaW19iDvnKz6uLWKOt81j0qyBWt7ZYX17jG5kf3eYmcyqlgtEmk5YAqhd5JVWNJNwdqP8FSYuOz36W1KED1VeE4649KRljMsLoEbzEVH1cVryqQGA1Y7kkYKW1vj1IPeAhwch12rX8k8dZAbMDcwkK+8BuzS9CrXajfVUVzE2WpZk//EXV/fduAb7xpq+1UMYO/hRXh7k2M3H+y106Xsre8G9uKJMIlzZj+MRWxS5sbta/LdKwX4Wv+oEa4WpKThcjtqtGF90UulJIvVPbSBWkVHwrafllfNnsVePHneCtJ1t7bjZMtKYDTOtPw/2IVo6iekbsKGKlSVDxQxB4sEIwF6tJH8SWgU0i99p/W3IGx050xiEId/I8lyxUkvDF5JEgOtalPznHtdIQfE4tcd8cKJVikQFBpCiyFcfSQMhDy5+xAgWA",
            PKey.PrivateKey2,
            PKey.Passphrase,
            false,
            null,
            null
        }; // wrong key
        yield return new object?[] {
            "AUhnrSEaW19iDvnKz6uLWKOt81j0qyBWt7ZYX17jG5kf3eYmcyqlgtEmk5YAqhd5JVWNJNwdqP8FSYuOz36W1KED1VeE4649KRljMsLoEbzEVH1cVryqQGA1Y7kkYKW1vj1IPeAhwch12rX8k8dZAbMDcwkK+8BuzS9CrXajfVUVzE2WpZk//EXV/fduAb7xpq+1UMYO/hRXh7k2M3H+y106Xsre8G9uKJMIlzZj+MRWxS5sbta/LdKwX4Wv+oEa4WpKThcjtqtGF90UulJIvVPbSBWkVHwrafllfNnsVePHneCtJ1t7bjZMtKYDTOtPw/2IVo6iekbsKGKlSVDxQxB4sEIwF6tJH8SWgU0i99p/W3IGx050xiEId/I8lyxUkvDF5JEgOtalPznHtdIQfE4tcd8cKJVikQFBpCiyFcfSQMhDy5+xAgWA",
            PKey.PrivateKey1,
            "djaofhsjkdqwi9494t798ahdnajd09q375qjwlakjfsjg",
            false,
            null,
            null
        }; // wrong passphrase
        yield return new object?[] {
            "AUhnrSEaW19iDvnKz6uLWKOt81j0qyBWt7ZYX17jG5kf3eYmcyqlgtEmk5YAqhd5JVWNJNwdqP8FSYuOz37W1KED1VeE4649KRljMsLoEbzEVH1cVryqQGA1Y7kkYKW1vj1IPeAhwch12rX8k8dZAbMDcwkK+8BuzS9CrXajfVUVzE2WpZk//EXV/fduAb7xpq+1UMYO/hRXh7k2M3H+y106Xsre8G9uKJMIlzZj+MRWxS5sbta/LdKwX4Wv+oEa4WpKThcjtqtGF90UulJIvVPbSBWkVHwrafllfNnsVePHneCtJ1t7bjZMtKYDTOtPw/2IVo6iekbsKGKlSVDxQxB4sEIwF6tJH8SWgU0i99p/W3IGx050xiEId/I8lyxUkvDF5JEgOtalPznHtdIQfE4tcd8cKJVikQFBpCiyFcfSQMhDy5+xAgWA",
            PKey.PrivateKey1,
            PKey.Passphrase,
            false,
            null,
            null
        }; // modified ciphertext
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
