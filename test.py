import socket
import json
import os
import sys
import time
import threading

def read_line(sock, initial=b'', timeout=120):
    buf = initial
    deadline = time.time() + timeout
    while time.time() < deadline:
        i = buf.find(b'\n')
        if i >= 0:
            line = buf[:i].decode('utf-8', 'ignore')
            rest = buf[i+1:]
            return line, rest
        try:
            data = sock.recv(4096)
        except socket.timeout:
            continue
        if not data:
            return None, b''
        buf += data
    return None, buf

def send_json(sock, obj, label=None):
    s = json.dumps(obj)
    if label:
        print(f"发送({label}): {s}")
    else:
        print(f"发送: {s}")
    sock.sendall((s + "\n").encode('utf-8'))

def send_raw(sock, text, label=None):
    if label:
        print(f"发送({label}): {text}")
    else:
        print(f"发送: {text}")
    sock.sendall((text + "\n").encode('utf-8'))

def receiver(sock, initial=b'', timeout=120, stop_flag=None):
    buf = initial
    while True:
        if stop_flag and stop_flag.is_set():
            return
        line, buf = read_line(sock, buf, timeout=1)
        if line:
            print(f"收到: {line}")

def interactive(sock, rest, timeout):
    stop = threading.Event()
    t = threading.Thread(target=receiver, args=(sock, rest, timeout, stop), daemon=True)
    t.start()
    print("进入交互模式，输入文本后回车发送，输入 exit 退出")
    try:
        while True:
            try:
                text = input()
            except EOFError:
                break
            if text is None:
                break
            text = text.strip()
            if not text:
                continue
            if text.lower() in ("exit", "quit"):
                break
            send_raw(sock, text, "手动")
    finally:
        stop.set()

def main():
    host = '127.0.0.1'
    port_env = os.environ.get('NEL_PORT')
    try:
        port = int(port_env) if port_env else 8080
    except:
        port = 8080
    if len(sys.argv) >= 2:
        host = sys.argv[1]
    if len(sys.argv) >= 3:
        try:
            port = int(sys.argv[2])
        except:
            pass
    timeout_env = os.environ.get('TEST_TIMEOUT')
    try:
        sock_timeout = int(timeout_env) if timeout_env else 120
    except:
        sock_timeout = 120
    step_timeout_env = os.environ.get('TEST_STEP_TIMEOUT')
    try:
        step_timeout = int(step_timeout_env) if step_timeout_env else 15
    except:
        step_timeout = 15

    account = os.environ.get('TEST_4399_ACCOUNT', '账号')
    password = os.environ.get('TEST_4399_PASSWORD', '密码')
    server_id = os.environ.get('TEST_SERVER_ID', '4661334467366178884')#服务器号，这里是布吉岛的
    server_name = os.environ.get('TEST_SERVER_NAME', '')
    role_env = os.environ.get('TEST_ROLE', '')

    s = socket.create_connection((host, port))
    s.settimeout(sock_timeout)

    line, rest = read_line(s, timeout=sock_timeout)
    if line:
        print(f"收到: {line}")

    send_json(s, {'ping':'ok'}, 'ping')
    line, rest = read_line(s, rest, timeout=sock_timeout)
    if line:
        print(f"收到: {line}")

    send_json(s, {'type':'noop'}, 'noop')
    line, rest = read_line(s, rest, timeout=sock_timeout)
    if line:
        print(f"收到: {line}")

    send_json(s, {'type':'login_4399','account':account,'password':password}, 'login_4399')
    buf = rest
    deadline = time.time() + step_timeout
    got_accounts = False
    while time.time() < deadline:
        line, buf = read_line(s, buf, timeout=2)
        if not line:
            continue
        print(f"收到: {line}")
        try:
            obj = json.loads(line)
            if isinstance(obj, dict) and obj.get('type') == 'accounts':
                got_accounts = True
                break
        except:
            pass
    if not got_accounts:
        print("在登录步骤未收到accounts，继续进行下一步")

    send_json(s, {'type':'open_server','serverId':server_id}, 'open_server')
    buf2 = buf
    role_id = role_env
    deadline2 = time.time() + step_timeout
    print(f"等待 server_roles 响应，最长 {sock_timeout} 秒")
    while time.time() < deadline2:
        line, buf2 = read_line(s, buf2, timeout=5)
        if not line:
            continue
        print(f"收到: {line}")
        try:
            obj = json.loads(line)
            if isinstance(obj, dict) and obj.get('type') == 'server_roles':
                items = obj.get('items') or []
                if not role_id and items:
                    role_id = items[0].get('id') or ''
                break
        except:
            pass
    if not role_id:
        print("未获取到角色ID，后续 join_game 可能失败")

    send_json(s, {'type':'join_game','serverId':server_id,'role':role_id,'serverName':server_name}, 'join_game')
    buf3 = buf2
    deadline3 = time.time() + step_timeout
    printed2 = 0
    print(f"等待 join_game 响应，最长 {sock_timeout} 秒")
    while time.time() < deadline3 and printed2 < 5:
        line, buf3 = read_line(s, buf3, timeout=5)
        if not line:
            continue
        print(f"收到: {line}")
        printed2 += 1

    interactive(s, buf3, sock_timeout)
    s.close()

if __name__ == '__main__':
    main()
