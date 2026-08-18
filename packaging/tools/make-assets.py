#!/usr/bin/env python3
"""
Sinh cac bien the cua logo tu file goc assets/tsudev-logo.png.

Chay tay khi logo goc thay doi:
    python3 packaging/tools/make-assets.py

Vi sao tu viet giai ma PNG thay vi dung Pillow: moi truong dung de dung du an
nay khong co thu vien anh, va them mot phu thuoc chi de doi kich thuoc mot file
la khong dang. Chi ho tro dung dinh dang cua file goc: PNG 8-bit RGBA, khong
xen ke (non-interlaced) - kiem tra va bao loi ro rang neu gap dinh dang khac.
"""
import struct, zlib, sys, os

def decode_png(path):
    d = open(path, "rb").read()
    if d[:8] != b"\x89PNG\r\n\x1a\n":
        raise SystemExit(f"{path}: khong phai file PNG")

    w = h = None
    idat = bytearray()
    i = 8
    while i < len(d):
        ln = struct.unpack(">I", d[i:i+4])[0]
        tag = d[i+4:i+8]
        body = d[i+8:i+8+ln]
        if tag == b"IHDR":
            w, h, bd, ct, comp, filt, il = struct.unpack(">IIBBBBB", body)
            if (bd, ct, il) != (8, 6, 0):
                raise SystemExit(f"{path}: chi ho tro PNG 8-bit RGBA khong xen ke "
                                 f"(gap bitdepth={bd} colortype={ct} interlace={il})")
        elif tag == b"IDAT":
            idat += body
        elif tag == b"IEND":
            break
        i += 12 + ln

    raw = zlib.decompress(bytes(idat))
    bpp, stride = 4, w * 4
    out = bytearray(w * h * 4)
    prev = bytearray(stride)
    pos = 0
    for y in range(h):
        ft = raw[pos]; pos += 1
        line = bytearray(raw[pos:pos+stride]); pos += stride
        if ft == 1:
            for x in range(bpp, stride): line[x] = (line[x] + line[x-bpp]) & 0xFF
        elif ft == 2:
            for x in range(stride): line[x] = (line[x] + prev[x]) & 0xFF
        elif ft == 3:
            for x in range(stride):
                a = line[x-bpp] if x >= bpp else 0
                line[x] = (line[x] + ((a + prev[x]) >> 1)) & 0xFF
        elif ft == 4:
            for x in range(stride):
                a = line[x-bpp] if x >= bpp else 0
                b = prev[x]
                c = prev[x-bpp] if x >= bpp else 0
                p = a + b - c
                pa, pb, pc = abs(p-a), abs(p-b), abs(p-c)
                pr = a if (pa <= pb and pa <= pc) else (b if pb <= pc else c)
                line[x] = (line[x] + pr) & 0xFF
        elif ft != 0:
            raise SystemExit(f"{path}: bo loc hang khong ho tro: {ft}")
        out[y*stride:(y+1)*stride] = line
        prev = line
    return w, h, out


def resize(w, h, px, nw, nh):
    """Thu nho bang trung binh o (box filter), nhan trong so theo alpha.

    Nhan trong so alpha la can thiet: neu trung binh RGB ma bo qua alpha, cac
    diem trong suot (thuong la mau den) se keo mau vien anh toi di, tao vien
    xam quanh logo.
    """
    out = bytearray(nw * nh * 4)
    for y in range(nh):
        y0, y1 = y*h//nh, max(y*h//nh + 1, (y+1)*h//nh)
        for x in range(nw):
            x0, x1 = x*w//nw, max(x*w//nw + 1, (x+1)*w//nw)
            r = g = b = a = n = 0
            for sy in range(y0, y1):
                base = sy*w*4
                for sx in range(x0, x1):
                    o = base + sx*4
                    al = px[o+3]
                    r += px[o] * al; g += px[o+1] * al; b += px[o+2] * al
                    a += al; n += 1
            o = (y*nw + x) * 4
            if a:
                out[o], out[o+1], out[o+2], out[o+3] = r//a, g//a, b//a, a//n
            else:
                out[o+3] = 0
    return out


def encode_png(w, h, px):
    raw = bytearray()
    for y in range(h):
        raw.append(0)
        raw += px[y*w*4:(y+1)*w*4]

    def chunk(tag, data):
        return (struct.pack(">I", len(data)) + tag + data
                + struct.pack(">I", zlib.crc32(tag + data) & 0xffffffff))

    return (b"\x89PNG\r\n\x1a\n"
            + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0))
            + chunk(b"IDAT", zlib.compress(bytes(raw), 9))
            + chunk(b"IEND", b""))


def encode_bmp(w, h, px, bg=(255, 255, 255)):
    """BMP 24-bit - dinh dang DUY NHAT ma Inno Setup nhan cho anh trinh thuat si.

    Khong co kenh alpha, nen phai tron san voi nen truoc.
    """
    row_pad = (-w * 3) % 4
    body = bytearray()
    for y in range(h - 1, -1, -1):              # BMP luu tu duoi len
        for x in range(w):
            o = (y*w + x) * 4
            al = px[o+3] / 255
            r = round(px[o]   * al + bg[0] * (1-al))
            g = round(px[o+1] * al + bg[1] * (1-al))
            b = round(px[o+2] * al + bg[2] * (1-al))
            body += bytes((b, g, r))            # BMP dung thu tu BGR
        body += b"\x00" * row_pad
    size = 54 + len(body)
    return (b"BM" + struct.pack("<IHHI", size, 0, 0, 54)
            + struct.pack("<IiiHHIIiiII", 40, w, h, 1, 24, 0, len(body), 2835, 2835, 0, 0)
            + bytes(body))


def paste(dst_w, dst_h, logo_w, logo_h, logo, at_x, at_y):
    canvas = bytearray(dst_w * dst_h * 4)
    for y in range(logo_h):
        ty = at_y + y
        if not (0 <= ty < dst_h): continue
        for x in range(logo_w):
            tx = at_x + x
            if not (0 <= tx < dst_w): continue
            s, t = (y*logo_w + x) * 4, (ty*dst_w + tx) * 4
            canvas[t:t+4] = logo[s:s+4]
    return canvas


if __name__ == "__main__":
    src = "assets/tsudev-logo.png"
    if not os.path.exists(src):
        raise SystemExit(f"Khong tim thay {src}")

    w, h, px = decode_png(src)
    print(f"nguon: {w}x{h}")

    # Ban nhe de NHUNG vao bao cao HTML. Bao cao phai xem duoc khi khong co
    # mang va khi copy sang may khac ma khong keo theo file phu nao, nen logo
    # di kem duoi dang data URI - do do kich thuoc rat dang quan tam.
    for px_h in (72, 144):
        nw = max(1, round(w * px_h / h))
        data = encode_png(nw, px_h, resize(w, h, px, nw, px_h))
        out = f"assets/tsudev-logo-{px_h}.png"
        open(out, "wb").write(data)
        print(f"  {out}: {nw}x{px_h}, {len(data)} byte")

    # Anh cho trinh thuat si cua Inno Setup - bat buoc la BMP.
    lw = round(w * 120 / h)
    small = resize(w, h, px, lw, 120)
    large = encode_bmp(164, 314, paste(164, 314, lw, 120, small, (164-lw)//2, 96))
    open("assets/wizard-large.bmp", "wb").write(large)
    print(f"  assets/wizard-large.bmp: 164x314, {len(large)} byte")

    # Icon cua ung dung va trinh cai dat, sinh TU CHINH LOGO.
    # Icon vuong nen logo (222x280, cao hon rong) duoc dat giua tren nen trong.
    ico_sizes = [16, 24, 32, 48, 64, 128, 256]
    images = []
    for sz in ico_sizes:
        lw = max(1, round(w * sz / h))
        scaled = resize(w, h, px, lw, sz)
        images.append(encode_png(sz, sz, paste(sz, sz, lw, sz, scaled, (sz - lw)//2, 0)))

    ico = bytearray(struct.pack("<HHH", 0, 1, len(ico_sizes)))
    offset = 6 + 16*len(ico_sizes)
    for sz, data in zip(ico_sizes, images):
        ico += struct.pack("<BBBBHHII", sz if sz < 256 else 0, sz if sz < 256 else 0,
                           0, 0, 1, 32, len(data), offset)
        offset += len(data)
    for data in images:
        ico += data
    open("assets/swico.ico", "wb").write(bytes(ico))
    print(f"  assets/swico.ico: {len(ico_sizes)} kich thuoc, {len(ico)} byte")

    lw2 = round(w * 50 / h)
    tiny = resize(w, h, px, lw2, 50)
    smallbmp = encode_bmp(55, 58, paste(55, 58, lw2, 50, tiny, (55-lw2)//2, 4))
    open("assets/wizard-small.bmp", "wb").write(smallbmp)
    print(f"  assets/wizard-small.bmp: 55x58, {len(smallbmp)} byte")
