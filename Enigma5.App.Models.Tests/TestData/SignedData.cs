/*
    Aenigma - Federated messaging system
    Copyright © 2023-2026 Romulus-Emanuel Ruja <romulus.ruja@aenigma.ro>

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

namespace Enigma5.App.Models.Tests.TestData;

// Values signed with the fixed test keys. A signature cannot be compared with a fresh one,
// because it is checked with the public key, not reproduced.
public static class SignedData
{
    // The text "shared-payload", signed with TestKeys.PrivateKey3.
    public const string SharedPayloadSignedWithKey3 =
        "c2hhcmVkLXBheWxvYWSOjVT6eFYtK/s2ABPox8rXm8gsb6L56EEsk1LdotEUd/H4pFjlsAZ6ySEZUvCOD9QbVOIGxKcOT+j+kenu+qFPg65Aqdy9DCR7h8jQQ71D3LV3C8PW3Frw4FRf+a79JsW+vj8UVeeT4ou0bx7KhDOJnY6eZjes5P3PDTkZgrIrXX2DeqC+Unpw1Sa1VmjS51A3z/ksJi7TXnX6olLhHmSsG4sIvF5tEEx3djqnHJawnmdjPpHo14uPi+dNHiBNRyT9QvXVfqqHhZBtLpTkVyyKmOGRD/owrciV6iRAah6zxgeMOlURWuLDOcGvd6PAw429Z0qbUCP6EEiuNT4RgmGW";

    // A neighborhood with Address1, the hostname http://adjacent.example and the neighbor Address2,
    // signed with TestKeys.PrivateKey1.
    public const string NeighborhoodSignedWithKey1 =
        "eyJBZGRyZXNzIjoiY2JmZjJlMTJmYjFmNzUyY2IxNzE4NWYwODBmMmI0MDMwMTE2NWExMDUxNTMxY2MwNjE0ZTQ5NWVlMjYyMGVmOSIsIkhvc3RuYW1lIjoiaHR0cDovL2FkamFjZW50LmV4YW1wbGUiLCJMYXN0VXBkYXRlIjoiMjAyNi0wMS0wMVQwMDowMDowMCswMDowMCIsIk5laWdoYm9ycyI6WyJhMTg2YTZmYmEwZmY3NTcwYjExNmIzZGY2MzllMzcxM2ZhYjBhMjFmMWNmNjJmYjYxNmQ4NGMxOTIxN2M4MDIzIl19d0nKZrLPf3/K+blnNivG7rAPskCRWh6fEnjs9dnkpujLo1FbARn0LEnrEfRkzCsxM5Yhc0C3n/l/qvn5Xwrxd+NVV1OVTlAywZgmPYSjgY8Fow63Tx7uWN20IiNOKc1h7/gYAkLke8ujj2x/e1e9ymtycKSJZ80HNwBCxcZPW9pinwFadab5OFmem9Jh/yODmGVc8Liep0TR2fKCwX8er7HUYRVCxhzcSZEhkyqGat9CSnYXIcY6RDs/uVgzj2mh2k9u9SnHnAcCDXejDkZyTczf4oBhvy79p9xEeubA1xJswO5NvjJIizu8L0cPrD2yxp+2DB+YhSwecu69xcMeGA==";

    // A neighborhood with Address1 and a neighbor that is not an address, signed with TestKeys.PrivateKey1.
    public const string NeighborhoodWithBadNeighborSignedWithKey1 =
        "eyJBZGRyZXNzIjoiY2JmZjJlMTJmYjFmNzUyY2IxNzE4NWYwODBmMmI0MDMwMTE2NWExMDUxNTMxY2MwNjE0ZTQ5NWVlMjYyMGVmOSIsIkxhc3RVcGRhdGUiOiIyMDI2LTAxLTAxVDAwOjAwOjAwKzAwOjAwIiwiTmVpZ2hib3JzIjpbIm5vdC1hbi1hZGRyZXNzIl197x4NLiHQBt8JtNvyAMId52pHcOMOsDw4St3Sq8cIKPzvDMlNk8H6deLDAxd2uvHlXDvCsHG99bfIyEu9el/yxxBHAarBwTcvjmEcSjoW3d1VMvhEymswu3lbn/z7sNZO+nemdrrQkV/sdUrQ4GDCqawQEgMtN0/xtTD12dVUP8geKtzCBf/M5NkdaU+aTWwkZm20mUBnOwiX/+i3h0B1Gjs+79PUl7WCsUMGUvFm/noEMMG3Ngh771eFS6pFVdveiTPFLc+wZr2h3nhneQWsJv0Ssdggr8EhoyatGa7rlJtKt9F58JJVbnwfc5qdMbkng+kDZgUz7ecCax8NkL241Q==";
}
