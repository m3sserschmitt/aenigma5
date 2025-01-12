/*
    Aenigma - Federal messaging system
    Copyright © 2024-2025 Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

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

namespace Enigma5.Crypto.Tests.TestData;

[ExcludeFromCodeCoverage]
public class UnsealerData : IEnumerable<object?[]>
{
    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator<object?[]> IEnumerable<object?[]>.GetEnumerator()
    {
        yield return new object[] {
            new byte[] { 31, 222, 59, 151, 46, 249, 125, 193, 75, 141, 111, 228, 215, 200, 232, 25, 130, 251, 56, 147, 192, 114, 204, 247, 210, 2, 107, 225, 41, 95, 219, 27, 3, 166, 203, 29, 112, 58, 206, 108, 206, 78, 110, 76, 112, 187, 85, 18, 104, 106, 148, 55, 169, 105, 69, 6, 112, 154, 122, 53, 181, 174, 38, 176, 132, 142, 71, 41, 96, 233, 110, 209, 219, 79, 17, 203, 249, 136, 162, 128, 200, 118, 214, 207, 190, 57, 209, 238, 163, 70, 16, 159, 0, 118, 28, 246, 113, 57, 205, 181, 58, 201, 222, 36, 215, 10, 122, 144, 33, 82, 246, 250, 105, 155, 94, 224, 177, 74, 96, 166, 239, 95, 55, 74, 198, 191, 241, 198, 145, 234, 23, 228, 226, 168, 194, 255, 22, 23, 203, 86, 200, 225, 99, 41, 119, 117, 58, 74, 13, 211, 117, 71, 148, 114, 248, 229, 232, 215, 142, 6, 247, 203, 54, 229, 153, 101, 112, 95, 21, 156, 18, 233, 81, 46, 254, 179, 85, 75, 226, 184, 35, 45, 13, 26, 241, 198, 254, 99, 85, 130, 84, 84, 19, 250, 71, 9, 4, 90, 35, 95, 162, 53, 224, 161, 8, 213, 218, 32, 96, 156, 227, 136, 164, 184, 65, 237, 103, 249, 69, 130, 189, 134, 222, 233, 154, 255, 1, 57, 250, 42, 55, 45, 163, 125, 117, 160, 145, 132, 6, 105, 27, 11, 215, 30, 128, 24, 129, 47, 137, 211, 190, 223, 183, 125, 163, 48, 3, 239, 30, 73, 116, 111, 136, 222, 204, 78, 164, 130, 243, 49, 247, 202, 4, 51, 140, 240, 108, 213, 144, 174, 187, 255, 58, 180, 47, 2, 169, 208, 118, 89, 235, 243, 36, 205, 18, 242 },
            PKey.PrivateKey1,
            PKey.Passphrase,
            new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x23, 0x56, 0x11 }
            };
        yield return new object[] {
            new byte[] { 6, 12, 127, 197, 64, 138, 65, 245, 106, 205, 232, 160, 193, 12, 88, 228, 49, 193, 78, 209, 176, 149, 239, 243, 94, 77, 243, 167, 101, 249, 181, 116, 219, 76, 182, 112, 15, 132, 112, 171, 208, 164, 208, 235, 51, 53, 82, 206, 249, 183, 29, 33, 21, 226, 117, 85, 138, 249, 31, 242, 231, 125, 224, 67, 79, 74, 122, 253, 10, 213, 123, 86, 58, 7, 213, 247, 224, 212, 12, 108, 208, 95, 123, 68, 225, 238, 124, 114, 164, 206, 224, 200, 36, 88, 246, 98, 24, 169, 160, 249, 165, 191, 41, 46, 80, 245, 182, 150, 22, 160, 105, 62, 79, 115, 76, 14, 144, 9, 83, 205, 121, 63, 124, 117, 45, 94, 224, 58, 82, 209, 234, 56, 245, 237, 75, 67, 209, 134, 12, 112, 157, 83, 72, 7, 62, 147, 83, 204, 93, 208, 54, 29, 211, 241, 168, 28, 21, 42, 79, 76, 34, 30, 186, 35, 112, 237, 169, 204, 112, 121, 188, 12, 206, 51, 50, 78, 184, 92, 220, 30, 53, 98, 125, 146, 49, 24, 12, 65, 230, 209, 133, 94, 170, 218, 229, 157, 29, 180, 122, 246, 21, 118, 183, 231, 230, 167, 248, 162, 55, 84, 185, 148, 52, 8, 160, 77, 167, 44, 109, 151, 12, 22, 66, 119, 94, 118, 195, 211, 252, 102, 121, 138, 149, 51, 8, 101, 252, 240, 83, 192, 59, 234, 157, 108, 81, 124, 209, 112, 11, 101, 10, 5, 48, 146, 148, 92, 156, 241, 221, 94, 189, 199, 90, 139, 115, 241, 81, 167, 79, 204, 131, 110, 106, 148, 253, 6, 213, 124, 24, 74, 230, 42, 97, 184, 229, 113, 2, 48, 103, 198, 99, 136 },
            PKey.PrivateKey3,
            string.Empty,
            new byte[] { 0x03, 0x04, 0x07, 0x01, 0x03, 0x02, 0x09, 0x07 }
        };
        yield return new object[] {
            new byte[] { 54, 55, 29, 159, 95, 236, 95, 14, 128, 46, 7, 255, 67, 113, 131, 197, 222, 81, 26, 242, 159, 129, 73, 7, 69, 18, 61, 117, 212, 144, 106, 7, 187, 139, 107, 240, 24, 189, 69, 48, 83, 223, 143, 55, 171, 65, 55, 18, 136, 72, 91, 64, 82, 231, 169, 49, 94, 116, 181, 186, 16, 169, 145, 164, 212, 128, 39, 22, 59, 38, 78, 207, 130, 183, 41, 194, 126, 127, 154, 163, 103, 36, 105, 220, 78, 232, 245, 152, 39, 196, 222, 57, 240, 110, 210, 113, 50, 148, 38, 88, 89, 254, 17, 27, 204, 72, 170, 82, 87, 158, 250, 101, 104, 69, 12, 126, 170, 31, 183, 14, 113, 110, 212, 226, 60, 202, 32, 40, 162, 61, 72, 93, 175, 220, 112, 26, 186, 84, 153, 231, 29, 80, 113, 141, 0, 205, 119, 136, 56, 175, 164, 15, 30, 48, 212, 31, 121, 213, 105, 155, 243, 165, 223, 202, 254, 207, 59, 68, 109, 91, 138, 209, 205, 246, 106, 50, 2, 14, 90, 2, 253, 52, 101, 134, 143, 239, 1, 20, 128, 89, 98, 222, 7, 246, 231, 46, 153, 84, 240, 250, 35, 213, 248, 246, 26, 17, 233, 28, 43, 141, 187, 242, 164, 115, 26, 243, 31, 198, 228, 241, 72, 77, 86, 46, 36, 188, 199, 174, 152, 131, 7, 97, 9, 246, 142, 252, 201, 73, 4, 87, 136, 141, 172, 57, 113, 80, 1, 24, 237, 117, 204, 154, 207, 111, 189, 17, 91, 140, 78, 62, 83, 218, 8, 192, 233, 126, 3, 15, 255, 147, 247, 73, 155, 32, 171, 123, 88, 229, 125, 44, 145, 132, 112, 142, 170, 246, 255, 124, 161, 58 },
            PKey.PrivateKey2,
            PKey.Passphrase,
            new byte[] { 0x05, 0x06, 0x07, 0x08, 0x03, 0x02 }
        };
        yield return new object?[] {
            new byte[] { 54, 55, 29, 159, 95, 236, 95, 14, 128, 46, 7, 255, 67, 113, 131, 197, 222, 81, 26, 242, 159, 129, 73, 5, 69, 18, 61, 117, 212, 144, 106, 7, 187, 139, 107, 240, 24, 189, 69, 48, 83, 223, 143, 55, 171, 65, 55, 18, 136, 72, 91, 64, 82, 231, 169, 49, 94, 116, 181, 186, 16, 169, 145, 164, 212, 128, 39, 22, 59, 38, 78, 207, 130, 183, 41, 194, 126, 127, 154, 163, 103, 36, 105, 220, 78, 232, 245, 152, 39, 196, 222, 57, 240, 110, 210, 113, 50, 148, 38, 88, 89, 254, 17, 27, 204, 72, 170, 82, 87, 158, 250, 101, 104, 69, 12, 126, 170, 31, 183, 14, 113, 110, 212, 226, 60, 202, 32, 40, 162, 61, 72, 93, 175, 220, 112, 26, 186, 84, 153, 231, 29, 80, 113, 141, 0, 205, 119, 136, 56, 175, 164, 15, 30, 48, 212, 31, 121, 213, 105, 155, 243, 165, 223, 202, 254, 207, 59, 68, 109, 91, 138, 209, 205, 246, 106, 50, 2, 14, 90, 2, 253, 52, 101, 134, 143, 239, 1, 20, 128, 89, 98, 222, 7, 246, 231, 46, 153, 84, 240, 250, 35, 213, 248, 246, 26, 17, 233, 28, 43, 141, 187, 242, 164, 115, 26, 243, 31, 198, 228, 241, 72, 77, 86, 46, 36, 188, 199, 174, 152, 131, 7, 97, 9, 246, 142, 252, 201, 73, 4, 87, 136, 141, 172, 57, 113, 80, 1, 24, 237, 117, 204, 154, 207, 111, 189, 17, 91, 140, 78, 62, 83, 218, 8, 192, 233, 126, 3, 15, 255, 147, 247, 73, 155, 32, 171, 123, 88, 229, 125, 44, 145, 132, 112, 142, 170, 246, 255, 124, 161, 58 },
            PKey.PrivateKey2,
            PKey.Passphrase,
            null // byte modified in ciphertext
        };
        yield return new object?[] {
            new byte[] { 31, 222, 59, 151, 46, 249, 125, 193, 75, 141, 111, 228, 215, 200, 232, 25, 130, 251, 56, 147, 192, 114, 204, 247, 210, 2, 107, 225, 41, 95, 219, 27, 3, 166, 203, 29, 112, 58, 206, 108, 206, 78, 110, 76, 112, 187, 85, 18, 104, 106, 148, 55, 169, 105, 69, 6, 112, 154, 122, 53, 181, 174, 38, 176, 132, 142, 71, 41, 96, 233, 110, 209, 219, 79, 17, 203, 249, 136, 162, 128, 200, 118, 214, 207, 190, 57, 209, 238, 163, 70, 16, 159, 0, 118, 28, 246, 113, 57, 205, 181, 58, 201, 222, 36, 215, 10, 122, 144, 33, 82, 246, 250, 105, 155, 94, 224, 177, 74, 96, 166, 239, 95, 55, 74, 198, 191, 241, 198, 145, 234, 23, 228, 226, 168, 194, 255, 22, 23, 203, 86, 200, 225, 99, 41, 119, 117, 58, 74, 13, 211, 117, 71, 148, 114, 248, 229, 232, 215, 142, 6, 247, 203, 54, 229, 153, 101, 112, 95, 21, 156, 18, 233, 81, 46, 254, 179, 85, 75, 226, 184, 35, 45, 13, 26, 241, 198, 254, 99, 85, 130, 84, 84, 19, 250, 71, 9, 4, 90, 35, 95, 162, 53, 224, 161, 8, 213, 218, 32, 96, 156, 227, 136, 164, 184, 65, 237, 103, 249, 69, 130, 189, 134, 222, 233, 154, 255, 1, 57, 250, 42, 55, 45, 163, 125, 117, 160, 145, 132, 6, 105, 27, 11, 215, 30, 128, 24, 129, 47, 137, 211, 190, 223, 183, 125, 163, 48, 3, 239, 30, 73, 116, 111, 136, 222, 204, 78, 164, 130, 243, 49, 247, 202, 4, 51, 140, 240, 108, 213, 144, 174, 187, 255, 58, 180, 47, 2, 169, 208, 118, 89, 235, 243, 36, 205, 18, 242 },
            PKey.PrivateKey2,
            PKey.Passphrase,
            null // wrong key
        };
        yield return new object?[] {
            new byte[] { 31, 222, 59, 151, 46, 249, 125, 193, 75, 141, 111, 228, 215, 200, 232, 25, 130, 251, 56, 147, 192, 114, 204, 247, 210, 2, 107, 225, 41, 95, 219, 27, 3, 166, 203, 29, 112, 58, 206, 108, 206, 78, 110, 76, 112, 187, 85, 18, 104, 106, 148, 55, 169, 105, 69, 6, 112, 154, 122, 53, 181, 174, 38, 176, 132, 142, 71, 41, 96, 233, 110, 209, 219, 79, 17, 203, 249, 136, 162, 128, 200, 118, 214, 207, 190, 57, 209, 238, 163, 70, 16, 159, 0, 118, 28, 246, 113, 57, 205, 181, 58, 201, 222, 36, 215, 10, 122, 144, 33, 82, 246, 250, 105, 155, 94, 224, 177, 74, 96, 166, 239, 95, 55, 74, 198, 191, 241, 198, 145, 234, 23, 228, 226, 168, 194, 255, 22, 23, 203, 86, 200, 225, 99, 41, 119, 117, 58, 74, 13, 211, 117, 71, 148, 114, 248, 229, 232, 215, 142, 6, 247, 203, 54, 229, 153, 101, 112, 95, 21, 156, 18, 233, 81, 46, 254, 179, 85, 75, 226, 184, 35, 45, 13, 26, 241, 198, 254, 99, 85, 130, 84, 84, 19, 250, 71, 9, 4, 90, 35, 95, 162, 53, 224, 161, 8, 213, 218, 32, 96, 156, 227, 136, 164, 184, 65, 237, 103, 249, 69, 130, 189, 134, 222, 233, 154, 255, 1, 57, 250, 42, 55, 45, 163, 125, 117, 160, 145, 132, 6, 105, 27, 11, 215, 30, 128, 24, 129, 47, 137, 211, 190, 223, 183, 125, 163, 48, 3, 239, 30, 73, 116, 111, 136, 222, 204, 78, 164, 130, 243, 49, 247, 202, 4, 51, 140, 240, 108, 213, 144, 174, 187, 255, 58, 180, 47, 2, 169, 208, 118, 89, 235, 243, 36, 205, 18, 242 },
            PKey.PrivateKey1,
            "mnjoifus0394iapsnfsoijffjsdgipjak",
            null // wrong passphrase
        };
    }
}
