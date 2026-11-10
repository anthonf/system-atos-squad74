"""QA isolado v3. Gera segredos em memoria; nunca reutiliza banco/container existente.
Executar da raiz com Python + cryptography, Docker, SDK .NET e dotnet-ef instalados.
Compilar API antes. Nao altera fontes, migrations ou configuracoes persistentes.
"""
import base64
import copy
import datetime as dt
import hashlib
import json
import os
from pathlib import Path
import secrets
import socket
import subprocess
import time
import traceback
import urllib.request
import urllib.error
from cryptography.hazmat.primitives.asymmetric import rsa, padding
from cryptography.hazmat.primitives import hashes, serialization

OUT = Path(__file__).resolve().parent
ROOT = OUT.parent
RUN = secrets.token_hex(5)
NAME = 'qa-v3-' + RUN
DB = 'qa_v3_' + RUN
ROLE = 'qa_v3'
PASSWORD = secrets.token_urlsafe(30)
APIKEY = secrets.token_urlsafe(32)
INVALID = secrets.token_urlsafe(32)
SENSITIVE = [PASSWORD, APIKEY, INVALID]
CID = None
API = None
BASE = None
RESULTS = []
TABLES = []
PHASE = 'sem mock 4/3'
LOG = None

def clean(s):
    for value in SENSITIVE:
        s = s.replace(value, '[OCULTO]')
    return s

def save(name, obj):
    (OUT/name).write_text(clean(json.dumps(obj,ensure_ascii=False,indent=2)),encoding='utf-8')

def say(s):
    s = clean(s)
    print(s,flush=True)
    if LOG:
        LOG.write(s+'\n'); LOG.flush()

def cmd(args, env=None, input=None):
    p = subprocess.run(args,cwd=ROOT,env=env,input=input,capture_output=True,text=True,encoding='utf-8',errors='replace')
    if p.returncode:
        raise RuntimeError(clean(p.stdout+'\n'+p.stderr))
    return p.stdout

def sql(q, as_json=True):
    assert CID and DB.startswith('qa_v3_')
    v = cmd(['docker','exec','-i',CID,'psql','-X','-U',ROLE,'-d',DB,'-v','ON_ERROR_STOP=1','-At'],input=q)
    return json.loads(v) if as_json else v

def snap():
    parts = ["'database',current_database()", "'role',current_user"]
    for t in TABLES:
        parts.append("'%s',coalesce((SELECT json_agg(x) FROM (SELECT * FROM \"%s\" ORDER BY 1) x),'[]'::json)" % (t,t))
    return sql('SELECT json_build_object('+','.join(parts)+');')

def delta(a,b):
    out = {}
    for t in TABLES:
        old = {str(x.get('id',x.get('MigrationId'))):x for x in a[t]}
        new = {str(x.get('id',x.get('MigrationId'))):x for x in b[t]}
        out[t] = dict(inserted=[v for k,v in new.items() if k not in old],deleted=[v for k,v in old.items() if k not in new],updated=[dict(before=old[k],after=v) for k,v in new.items() if k in old and old[k]!=v])
    return out

def http(method,path,body=None,headers=None):
    req = urllib.request.Request(BASE+path,data=json.dumps(body).encode() if body is not None else None,headers={'Content-Type':'application/json',**(headers or {})},method=method)
    try:
        r = urllib.request.urlopen(req,timeout=20)
    except urllib.error.HTTPError as e:
        r = e
    text = r.read().decode()
    try: parsed=json.loads(text)
    except ValueError: parsed=text
    return dict(status=r.status,headers=dict(r.headers),body=parsed,raw_body=text)

def record(id,objective,expected,section='2.1, 6.2; RF01'):
    return dict(id=id,requirement=section,objective=objective,expected=expected,phase=PHASE,preconditions='Banco exclusivo confirmado; SLA QA/Valid ativo. Fixtures e ausencia/existencia de IDs documentadas no snapshot before. Sem JWT salvo quando explicito.',attempts=[])

def request(rec,body=None,headers=None,method='POST',path='/api/chamados',variant='principal'):
    before=snap()
    response=http(method,path,body,headers if headers is not None else {'X-Api-Key':APIKEY})
    after=snap()
    a=dict(variant=variant,time_utc=dt.datetime.now(dt.timezone.utc).isoformat(),request=dict(method=method,url=BASE+path,headers={k:('[JWT QA OCULTO]' if k.lower()=='authorization' else ('[CHAVE QA VALIDA OCULTA]' if v==APIKEY else '[CHAVE QA INVALIDA OCULTA]' if v==INVALID else v)) for k,v in (headers if headers is not None else {'X-Api-Key':APIKEY}).items()},body=body),response=response,before=before,after=after,differences=delta(before,after))
    # Toda criacao recebe confirmacao SQL e GET real, inclusive criacoes indevidas.
    if method=='POST' and 200<=response['status']<300 and isinstance(response['body'],dict):
        code=response['body'].get('codigoPublico')
        rows=[c for c in after['chamado'] if c['codigo_publico']==code]
        get=http('GET','/api/chamados/'+str(code),headers={'X-Api-Key':APIKEY})
        same=bool(rows and rows[0]['id']==response['body'].get('id'))
        detail=get['body'].get('chamado',{}) if isinstance(get['body'],dict) else {}
        match=all(detail.get(k)==response['body'].get(k) for k in ['id','codigoPublico','produto','categoria','descricao','usuarioId','grupoEmpresaId'])
        a['public_code_verification']=dict(code=code,sql_rows=rows,retrieval=get,consistent=same and get['status']==200 and match)
    rec['attempts'].append(a)
    say(json.dumps(dict(id=rec['id'],variant=variant,method=method,path=path,headers=a['request']['headers'],body=body,status=response['status'],response=response['body'],changes={k:{z:len(v) for z,v in d.items()} for k,d in a['differences'].items()}),ensure_ascii=False))
    return a

def finish(rec,status,why):
    rec.update(classification=status,justification=why,evidence=rec['id']+'.json')
    RESULTS.append(rec)
    save(rec['id']+'.json',rec)
    save('results.json',RESULTS)
    say(rec['id']+' => '+status+' — '+why)

def verdict(rec,ok,why):
    finish(rec,'APROVADO' if ok else 'REPROVADO',why)

def nochange(a): return a['before']==a['after']
def success(a): return 200<=a['response']['status']<300 and a.get('public_code_verification',{}).get('consistent',False)
def linked(a,u,g):
    b=a['response']['body']
    return success(a) and b.get('usuarioId')==u and b.get('grupoEmpresaId')==g
def find(a,t,id): return next((x for x in a[t] if x['id']==id),None)

def body(uid=61001,gid=71001,name='QA V3 A',email='a@qa.example.invalid',group='QA V3 Grupo A'):
    return dict(produto='QA',categoria='Valid',descricao='Descricao QA v3',mensagemInicial='Descricao QA v3',titulo='QA v3',usuario=dict(id=uid,nome=name,email=email),grupoEmpresa=dict(id=gid,nome=group))

def identity_ok(a,b):
    u,g=b['usuario'],b['grupoEmpresa']
    ur=find(a['after'],'usuario',u['id']); gr=find(a['after'],'grupo_empresa',g['id'])
    return bool(linked(a,u['id'],g['id']) and ur and gr and ur['nome']==u['nome'] and ur['email']==u['email'] and ur['grupo_empresa_id']==g['id'] and gr['nome']==g['nome'])

def fingerprints():
    paths=cmd(['git','ls-files','-z']).split('\0')
    paths += [str(x.relative_to(ROOT)) for x in (ROOT/'qa-rf01-20261103').rglob('*') if x.is_file()]
    paths += ['Especificacao do desafio/Termo_Referencia_Residencia_Software.v3.docx']
    return {p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest() if (ROOT/p).is_file() else None for p in sorted(set(paths)) if p}

def prepare():
    global CID,TABLES,API,BASE
    save('original-integrity-before.json',fingerprints())
    assert not (OUT/'initial-state.json').exists(), 'Nao sobrescrever execucao existente'
    env=dict(os.environ,POSTGRES_USER=ROLE,POSTGRES_PASSWORD=PASSWORD,POSTGRES_DB=DB)
    CID=cmd(['docker','run','--detach','--name',NAME,'--label','qa-v3-run='+RUN,'--publish','127.0.0.1::5432','--env','POSTGRES_USER','--env','POSTGRES_PASSWORD','--env','POSTGRES_DB','--tmpfs','/var/lib/postgresql/data','postgres:17'],env=env).strip()
    d=json.loads(cmd(['docker','inspect',CID]))[0]
    assert d['Config']['Labels']['qa-v3-run']==RUN and not d['Mounts']
    assert '/var/lib/postgresql/data' in d['HostConfig']['Tmpfs']
    port=d['NetworkSettings']['Ports']['5432/tcp'][0]
    assert port['HostIp']=='127.0.0.1'
    for _ in range(40):
        p=subprocess.run(['docker','exec',CID,'pg_isready','-U',ROLE,'-d',DB],capture_output=True)
        if p.returncode==0: break
        time.sleep(.5)
    else: raise RuntimeError('PostgreSQL indisponivel')
    check=sql("SELECT json_build_object('database',current_database(),'role',current_user,'public_tables',(SELECT count(*) FROM information_schema.tables WHERE table_schema='public'));")
    assert check==dict(database=DB,role=ROLE,public_tables=0)
    save('isolation.json',dict(container_id=CID,container_name=NAME,label=RUN,image=d['Config']['Image'],mounts=d['Mounts'],tmpfs=d['HostConfig']['Tmpfs'],port=port,before_writes=check,external_services='Nenhum cliente HTTP/webhook de saida registrado na inicializacao/caso de uso inspecionado; sem secrets externos; ambiente Production. Somente chamadas locais.'))
    say('ISOLAMENTO CONFIRMADO ANTES DE MIGRATIONS/FIXTURES: '+json.dumps(check))
    conn=f"Host=127.0.0.1;Port={port['HostPort']};Database={DB};Username={ROLE};Password={PASSWORD}"
    key=rsa.generate_private_key(public_exponent=65537,key_size=2048)
    public=key.public_key().public_bytes(serialization.Encoding.PEM,serialization.PublicFormat.SubjectPublicKeyInfo).decode()
    with socket.socket() as sock:
        sock.bind(('127.0.0.1',0)); apiport=sock.getsockname()[1]
    BASE=f'http://127.0.0.1:{apiport}'
    env=dict(os.environ,ASPNETCORE_ENVIRONMENT='Production',ASPNETCORE_URLS=BASE,ConnectionStrings__DefaultConnection=conn,Sso__ApiKey=APIKEY,Sso__Issuer='qa-v3',Sso__Audience='qa-v3',Sso__PublicKeyPem=public,Logging__EventLog__LogLevel__Default='None')
    # Sem configuracoes de ambiente herdadas para outros bancos/servicos.
    allowed={'ASPNETCORE_ENVIRONMENT','ASPNETCORE_URLS','ConnectionStrings__DefaultConnection','Sso__ApiKey','Sso__Issuer','Sso__Audience','Sso__PublicKeyPem','Logging__EventLog__LogLevel__Default'}
    for k in list(env):
        if ('__' in k or k.startswith('ASPNETCORE_')) and k not in allowed: del env[k]
    bindir=ROOT/'src/backend/src/SupportAtos.Api/bin/Debug/net10.0'
    ef=list((Path.home()/'.dotnet/tools/.store/dotnet-ef').rglob('ef.dll'))
    if not ef: raise RuntimeError('dotnet-ef instalado nao encontrado')
    args=['dotnet','exec','--depsfile',str(bindir/'SupportAtos.Api.deps.json'),'--runtimeconfig',str(bindir/'SupportAtos.Api.runtimeconfig.json'),str(ef[-1]),'database','update','--assembly',str(bindir/'SupportAtos.Infrastructure.dll'),'--startup-assembly',str(bindir/'SupportAtos.Api.dll'),'--project-dir',str(bindir),'--connection',conn]
    (OUT/'migrations.log').write_text(clean(cmd(args,env=env)),encoding='utf-8')
    TABLES=sql("SELECT coalesce(json_agg(tablename ORDER BY tablename),'[]'::json) FROM pg_tables WHERE schemaname='public';")
    save('schema.json',sql("SELECT json_build_object('columns',(SELECT json_agg(c) FROM (SELECT table_name,column_name,data_type,is_nullable,column_default FROM information_schema.columns WHERE table_schema='public' ORDER BY table_name,ordinal_position)c),'constraints',(SELECT json_agg(c) FROM (SELECT conname,contype,convalidated,pg_get_constraintdef(oid) AS definition FROM pg_constraint WHERE connamespace='public'::regnamespace ORDER BY conname)c));"))
    save('empty-migrated-state.json',snap())
    fixtures="""INSERT INTO grupo_empresa(id,nome,codigo_externo,tipo,criado_em) VALUES
    (71001,'QA V3 Grupo A','v3-grupo-a','matriz',now()),(71002,'QA V3 Grupo B','v3-grupo-b','matriz',now()),(3301,'QA V3 Legado','95001','matriz',now());
    INSERT INTO usuario(id,nome,email,codigo_externo,grupo_empresa_id,papel,criado_em) VALUES
    (61001,'QA V3 A','a@qa.example.invalid','v3-a',71001,'Cliente',now()),(61002,'QA V3 B','b@qa.example.invalid','v3-b',71002,'Cliente',now()),(61003,'QA V3 C','c@qa.example.invalid','v3-c',71001,'Cliente',now()),(2301,'QA V3 Legado','legado@qa.example.invalid','94001',3301,'Cliente',now());
    INSERT INTO sla_config(id,produto,categoria,prioridade,tempo_resposta_min,tempo_resolucao_min,ativo) VALUES(1,'QA','Valid','Media',60,480,true);"""
    (OUT/'fixtures.sql').write_text(fixtures,encoding='utf-8')
    sql(fixtures,False)
    save('initial-state.json',snap())
    now=int(time.time())
    claims=dict(sub='qa-jwt-only',name='QA JWT',email='jwt@qa.example.invalid',grupo_empresa_id='qa-jwt-group',grupo_empresa_nome='QA JWT Group',role='Cliente',iss='qa-v3',aud='qa-v3',nbf=now-60,iat=now,exp=now+3600)
    enc=lambda x:base64.urlsafe_b64encode(x).rstrip(b'=')
    unsigned=enc(json.dumps(dict(alg='RS256',typ='JWT')).encode())+b'.'+enc(json.dumps(claims).encode())
    sig=key.sign(unsigned,padding.PKCS1v15(),hashes.SHA256())
    key.public_key().verify(sig,unsigned,padding.PKCS1v15(),hashes.SHA256())
    jwt=(unsigned+b'.'+enc(sig)).decode(); SENSITIVE.append(jwt)
    save('jwt-metadata.json',dict(claims=claims,public_key=public,signature_locally_verified=True,used_only_for_negative_auth_tests=True))
    API=subprocess.Popen(['dotnet',str(bindir/'SupportAtos.Api.dll')],cwd=ROOT,env=env,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
    # Ler o pipe em thread evita bloqueio do processo em execucoes extensas.
    import threading
    def pump():
        with open(OUT/'api.log','w',encoding='utf-8') as log:
            for line in iter(API.stdout.readline,b''):
                log.write(clean(line.decode('utf-8',errors='replace')));log.flush()
    threading.Thread(target=pump,daemon=True).start()
    for _ in range(50):
        if API.poll() is not None: raise RuntimeError('API encerrou ao iniciar; consultar api.log')
        try:
            if http('GET','/health')['status']==200: break
        except OSError: time.sleep(.3)
    else: raise RuntimeError('API nao iniciou')
    save('runtime.json',dict(api_pid=API.pid,base_url=BASE,environment='Production',database=DB,role=ROLE,auth_credentials='Aleatorias, apenas em memoria e variaveis dos processos; nenhuma em arquivos.',git_head=cmd(['git','rev-parse','HEAD']).strip()))
    return jwt

def execute(jwt):
    global PHASE
    # Primeira tentativa: usuario/grupo novos sem os IDs do provider mock no banco.
    r=record('RF10-01','Usuario e grupo inexistentes','Criar ambos na abertura, com IDs 61090/71090, e vincular o chamado.','2.5, 6.2, 8; RF10')
    b=body(61090,71090,'QA Novo','novo@qa.example.invalid','QA Grupo Novo')
    a=request(r,b)
    verdict(r,identity_ok(a,b),'Entrada completa aninhada; verificar HTTP e SQL. Sem fixture 4/3: entidades solicitadas devem nascer automaticamente; nao houve correcao da aplicacao.')
    # Fixture de compatibilidade para prosseguir nos demais testes apesar da dependencia fixa.
    before=snap()
    sql("INSERT INTO grupo_empresa(id,nome,codigo_externo,tipo,criado_em) VALUES(3,'QA Mock Grupo','qa-mock-grupo','matriz',now()); INSERT INTO usuario(id,nome,email,codigo_externo,grupo_empresa_id,papel,criado_em) VALUES(4,'QA Mock','mock@qa.example.invalid','qa-mock',3,'Admin',now());",False)
    PHASE='com fixture de compatibilidade mock 4/3'
    save('compatibility-fixture.json',dict(reason='Permitir testes independentes apos primeira tentativa sem IDs fixos; dados apenas no banco QA.',before=before,after=snap(),sql="INSERT grupo_empresa id=3; INSERT usuario id=4 grupo_empresa_id=3"))
    neg=[('AUTH-02',{}),('AUTH-03',{'X-Api-Key':INVALID}),('AUTH-04',{'X-Api-Key':''}),('AUTH-05',{'Authorization':'Bearer '+jwt}),('AUTH-06',{'X-Api-Key':INVALID,'Authorization':'Bearer '+jwt})]
    for id,h in [('AUTH-01',{'X-Api-Key':APIKEY}),*neg,('AUTH-07',{'X-Api-Key':APIKEY})]:
        positive=id in ['AUTH-01','AUTH-07']
        r=record(id,'API Key '+('valida sem JWT' if positive else 'ausente/invalida/vazia ou JWT isolado'),'Processamento sem depender de JWT (vinculo avaliado separadamente).' if positive else 'HTTP 401 e nenhuma alteracao em qualquer tabela.','6.2; autenticacao')
        a=request(r,body(),h)
        verdict(r,success(a) if positive else a['response']['status']==401 and nochange(a),'Resultado da autenticacao observada; sucesso isolado com chave correta nao comprova validacao da chave. Vínculo/RF10 avaliados separadamente.' if positive else 'Comparacao com 401 expresso e snapshots de todas as tabelas.')
    r=record('AUTH-08','Negativos com corpo v3 completo e entidades novas','Todas as variantes retornam 401 sem usuario, grupo, chamado ou interacao novos.','6.2; autenticacao')
    aa=[]
    for i,(id,h) in enumerate(neg):
        aa.append(request(r,body(62000+i,72000+i,f'QA Auth {i}',f'auth{i}@qa.example.invalid',f'QA Auth Grupo {i}'),h,variant=id))
    verdict(r,all(a['response']['status']==401 and nochange(a) for a in aa),'Cinco variantes com identidades inexistentes no corpo; comparar SQL antes/depois. JWT nao substitui a chave.')
    first=None
    for id in ['RF01-%02d'%i for i in range(1,15)]:
        r=record(id,'Criacao/contrato/associacao '+id,'Conforme criterio individual descrito na justificativa.')
        b=body()
        if id=='RF01-01':
            r['expected']='Criar com codigo publico persistido e associado aos IDs 61001/71001 enviados no corpo.'
            a=request(r,b);first=a
            verdict(r,identity_ok(a,b),'Abertura completa: codigo e persistencia conferidos, alem da identidade exigida; sucesso HTTP isolado nao basta.')
        elif id in ['RF01-02','RF01-03','RF01-04']:
            field={'RF01-02':'produto','RF01-03':'categoria','RF01-04':'descricao'}[id]
            b.pop(field)
            if id=='RF01-04': b.pop('mensagemInicial')
            r['expected']='Rejeitar ausencia de '+field+' sem gravacoes; v3 nao fixa codigo HTTP.'
            a=request(r,b)
            verdict(r,400<=a['response']['status']<500 and nochange(a),'Campo obrigatorio ausente, demais dados v3 presentes; mensagemInicial removida tambem em RF01-04 para nao enviar descricao por alias.')
        elif id=='RF01-05':
            b.pop('mensagemInicial');r['expected']='Avaliar se descricao preenchida e suficiente; v3 nao nomeia propriedade JSON.'
            a=request(r,b)
            finish(r,'NÃO VERIFICADO','Resposta observada registrada; exigir mensagemInicial adicional e incompatibilidade potencial, mas a v3 nao fixa nome JSON e falta contrato acordado para julgar este nome isoladamente.')
        elif id in ['RF01-06','RF01-07']:
            if id=='RF01-06': b.pop('titulo')
            r['expected']='Aceitar omissao de titulo (opcional no DTO) ou anexos (opcionais expressamente na v3); codigo deve estar persistido.'
            a=request(r,b)
            verdict(r,success(a),'Aprovacao restrita a opcionalidade e codigo/persistencia; associacao correta nao esta incluida nesta aprovacao e e julgada nos cenarios proprios.')
        elif id=='RF01-08':
            r['expected']='Utilizar dados do corpo e vincular aos IDs 61001/71001; testar duas estruturas plausiveis, sem defini-las como contrato oficial.'
            a=request(r,b)
            flat={k:v for k,v in b.items() if k not in ['usuario','grupoEmpresa']}
            flat.update(usuarioId=61002,usuarioNome='QA V3 B',usuarioEmail='b@qa.example.invalid',grupoEmpresaId=71002,grupoEmpresaNome='QA V3 Grupo B')
            alt=request(r,flat,variant='estrutura-plana')
            verdict(r,linked(a,61001,71001) and linked(alt,61002,71002),'Campos nao reconhecidos recebem 201, mas nenhuma das duas estruturas controla o vinculo gravado. Conclusao limitada aos formatos enviados; formato oficial nao esta especificado.')
        elif id in ['RF01-09','RF01-10']:
            b.pop('usuario' if id=='RF01-09' else 'grupoEmpresa')
            r['expected']='Rejeitar ausencia dos dados obrigatorios de identidade, sem persistencia indevida; sem status exato fixado pela v3.'
            a=request(r,b)
            verdict(r,400<=a['response']['status']<500 and nochange(a),'Corpo sem usuario ou grupo nao deve originar chamado com identidade inventada/fixa.')
        elif id=='RF01-11':
            r['expected']='Registrar ambas as combinacoes sem SLA, sem presumir regra normativa de rejeicao.'
            request(r,{**b,'produto':'QA Sem SLA'},variant='produto')
            request(r,{**b,'categoria':'QA Sem SLA'},variant='categoria')
            finish(r,'NÃO VERIFICADO','Comportamento executado e registrado; a v3 nao determina o resultado para ausencia de SLA na abertura.')
        elif id=='RF01-12':
            r['expected']='GET do chamado criado deve corresponder ao codigo e conteudo realmente persistidos; nao avalia RF02.'
            code=first['response']['body'].get('codigoPublico') if success(first) else None
            if not code: finish(r,'BLOQUEADO','Sem codigo de criacao disponivel para esta consulta.');continue
            a=request(r,method='GET',path='/api/chamados/'+code)
            ch=a['response']['body'].get('chamado',{})
            row=next((x for x in a['after']['chamado'] if x['codigo_publico']==code),None)
            verdict(r,a['response']['status']==200 and bool(row) and ch.get('id')==row['id'] and ch.get('descricao')==row['descricao'] and nochange(a),'Consulta auxiliar de persistencia. Identidade de negocio continua avaliada separadamente.')
        elif id=='RF01-13':
            r['expected']='Cada chamado deve ter a identidade do seu corpo ao alternar clientes/grupos.'
            a=request(r,b,variant='cliente-A')
            other=body(61002,71002,'QA V3 B','b@qa.example.invalid','QA V3 Grupo B')
            z=request(r,other,variant='cliente-B')
            verdict(r,identity_ok(a,b) and identity_ok(z,other),'Comparacao de dados explicitamente no corpo, sem JWT. Mistura com usuario fixo constitui causa comum.')
        else:
            b=body(94001,95001,'QA Portal ID','portal@qa.example.invalid','QA Portal Grupo')
            r['expected']='IDs 94001/95001 devem ser os IDs reais das entidades e do vinculo; codigo_externo das fixtures 2301/3301 nao substitui a regra.'
            a=request(r,b)
            verdict(r,identity_ok(a,b),'Fixture possui IDs internos 2301/3301 e codigo_externo 94001/95001. O teste verifica IDs reais, nao equivalencia por codigo_externo.')
    # Upsert: cada alteracao tem snapshot proprio; nao ha updates SQL para simular sucesso.
    for n in range(2,13):
        id='RF10-%02d'%n;r=record(id,'Sincronizacao automatica '+id,'Criar/atualizar entidades corretas, preservar IDs e nao duplicar.','2.5, 6.2, 8; RF10')
        b=body()
        if n==3: b=body(61091,71001,'QA Usuario Novo Existente','novo-existente@qa.example.invalid')
        if n==4: b['usuario']['nome']='QA V3 A Atualizado'
        if n==5: b['usuario']['email']='a-atualizado@qa.example.invalid'
        if n==6: b['grupoEmpresa']['nome']='QA V3 Grupo A Atualizado'
        if n==10: b=body(61092,71092,'QA IDs Novos','ids@qa.example.invalid','QA Grupo IDs Novos')
        if n==11:
            b['usuario']['nome']='NAO DEVE ATUALIZAR';b['grupoEmpresa']['nome']='NAO DEVE ATUALIZAR'
            a=request(r,b,{'X-Api-Key':INVALID})
            verdict(r,a['response']['status']==401 and nochange(a),'Falha de autenticacao deve impedir qualquer alteracao; 201 com chamado novo reprova mesmo sem update de usuario/grupo.');continue
        if n==12:
            b=body(61093,71093,'QA Validacao Nova','validacao@qa.example.invalid','QA Grupo Validacao');b.pop('categoria')
            a=request(r,b)
            verdict(r,400<=a['response']['status']<500 and nochange(a),'Falha de validacao com identidade nova nao deve deixar entidades parciais. Nao implica que upsert em sucesso funcione.');continue
        a=request(r,b,variant='primeira')
        if n in [7,8,9]:
            other=copy.deepcopy(b)
            if n==8: other=body(61003,71001,'QA V3 C','c@qa.example.invalid')
            if n==9: other=body(61002,71002,'QA V3 B','b@qa.example.invalid','QA V3 Grupo B')
            z=request(r,other,variant='repeticao' if n==7 else 'identidade-alternada')
            if n==7:
                r['expected']='Nao duplicar usuario/grupo ja existentes ao repetir abertura com identidade igual. Nao exigir idempotencia do chamado.'
                verdict(r,success(a) and success(z) and a['before']['usuario']==z['after']['usuario'] and a['before']['grupo_empresa']==z['after']['grupo_empresa'],'Sem duplicacao de entidades existentes demonstrada; aprovacao apenas dessa propriedade, nao de associacao ou capacidade de atualizar.')
            else: verdict(r,identity_ok(a,b) and identity_ok(z,other),'Associacao deve seguir cada identidade enviada no corpo; usuarios distintos no mesmo grupo ou em grupos diferentes.')
        else:
            verdict(r,identity_ok(a,b),'Comparacao dos nomes, emails, IDs reais, grupo do usuario e vinculo do chamado com o corpo. A ausencia de modificacao diante de valores alterados e evidencia funcional, nao so estatica.')
    integration()

def integration():
    # Verificacoes reais SQL/GET consolidadas em seis cenarios, sem expandir RF02.
    r=record('INT-01','Chaves estrangeiras e vinculos estruturais','Zero orfaos e chamado/interacao com FKs consistentes; identidade do portal e avaliada em INT-05.','RF01/RF10; integridade derivada de 2.1, 2.5, 6.2')
    s=snap()
    checks=sql("SELECT json_build_object('chamado_usuario_orfao',(SELECT count(*) FROM chamado c LEFT JOIN usuario u ON u.id=c.usuario_id WHERE u.id IS NULL),'chamado_grupo_orfao',(SELECT count(*) FROM chamado c LEFT JOIN grupo_empresa g ON g.id=c.grupo_empresa_id WHERE g.id IS NULL),'usuario_grupo_diverge',(SELECT count(*) FROM chamado c JOIN usuario u ON u.id=c.usuario_id WHERE c.grupo_empresa_id<>u.grupo_empresa_id),'interacao_orfa',(SELECT count(*) FROM interacao i LEFT JOIN chamado c ON c.id=i.chamado_id LEFT JOIN usuario u ON u.id=i.autor_id WHERE c.id IS NULL OR u.id IS NULL));")
    r.update(sql_checks=checks,before=s,after=snap(),source_evidence=[x['evidence'] for x in RESULTS])
    verdict(r,all(v==0 for v in checks.values()),'Integridade referencial estrutural, distinta de corretude da identidade de negocio.')
    r=record('INT-02','Ausencia de gravacoes parciais em criacoes rejeitadas','Todas as respostas rejeitadas devem preservar os dados anteriores, incluindo usuario e grupo.','RF01/RF10; integridade')
    rejected=[(x['id'],a) for x in RESULTS for a in x['attempts'] if a['response']['status']>=400]
    r.update(before=snap(),source_evidence=[id+'.json' for id,a in rejected],checks=[dict(id=id,status=a['response']['status'],no_changes=nochange(a)) for id,a in rejected],after=snap())
    verdict(r,bool(rejected) and all(nochange(a) for id,a in rejected),'Inclui falha 500 inicial e validacoes 400; autenticacao que retornou 201 indevidamente e julgada em AUTH/RF10-11, nao como rejeicao.')
    r=record('INT-03','Identificadores publicos','Todos os chamados criados devem ter codigo unico, persistido e recuperavel.','2.1; RF01')
    r['before']=snap(); codes=[]; retrievals=[]
    for row in r['before']['chamado']:
        codes.append(row['codigo_publico'])
        res=http('GET','/api/chamados/'+row['codigo_publico'],headers={'X-Api-Key':APIKEY})
        retrievals.append(dict(request=dict(method='GET',url=BASE+'/api/chamados/'+row['codigo_publico'],headers={'X-Api-Key':'[CHAVE QA VALIDA OCULTA]'}),response=res,sql_row=row))
        say('INT-03 GET '+row['codigo_publico']+' '+str(res['status']))
    r.update(retrievals=retrievals,after=snap())
    verdict(r,bool(codes) and all(codes) and len(codes)==len(set(codes)) and all(x['response']['status']==200 and x['response']['body']['chamado']['id']==x['sql_row']['id'] for x in retrievals),'GET real para cada chamado final; verificacao auxiliar de RF01, sem auditoria de RF02.')
    r=record('INT-04','Entidades necessarias antes de associar','Usuario e grupo novos enviados devem existir e sustentar o vinculo do chamado apos abertura bem sucedida.','2.5, 6.2; RF01/RF10')
    source=next(x for x in RESULTS if x['id']=='RF10-10');a=source['attempts'][0]
    r.update(source_evidence=['RF10-01.json','RF10-10.json'],before=a['before'],after=a['after'])
    verdict(r,identity_ok(a,a['request']['body']),'Nao se deduz ordem temporal interna por timestamp. Observa-se a pos-condicao: abertura nao deve associar outro usuario enquanto as entidades solicitadas nem existem.')
    r=record('INT-05','Isolamento de identidade entre clientes','Chamados A/B devem apontar para respectivos IDs do corpo, sem mistura.','RF01; 6.2')
    source=next(x for x in RESULTS if x['id']=='RF01-13')
    r.update(source_evidence=['RF01-13.json'],before=source['attempts'][0]['before'],after=source['attempts'][-1]['after'])
    verdict(r,all(identity_ok(a,a['request']['body']) for a in source['attempts']),'Reutiliza duas requisicoes reais A/B desta bateria; nao e auditoria completa de RF02.')
    r=record('INT-06','Repeticao sem duplicar entidades','Repeticao pode criar chamados distintos, mas nao duplicar usuario/grupo existentes.','2.5, 6.2; RF10')
    source=next(x for x in RESULTS if x['id']=='RF10-07')
    r.update(source_evidence=['RF10-07.json'],before=source['attempts'][0]['before'],after=source['attempts'][-1]['after'])
    verdict(r,r['before']['usuario']==r['after']['usuario'] and r['before']['grupo_empresa']==r['after']['grupo_empresa'],'Aprovacao restrita a nao duplicacao de fixtures existentes; nao prova upsert de novos nem atualizacao. Sem exigir idempotencia do chamado.')

def cleanup():
    state={}
    if API:
        API.terminate()
        try: API.wait(timeout=15)
        except subprocess.TimeoutExpired: API.kill();API.wait(timeout=5)
        state['api']=dict(pid=API.pid,exit_code=API.returncode,terminated=True)
    if CID:
        d=json.loads(cmd(['docker','inspect',CID]))[0]
        assert d['Id']==CID and d['Config']['Labels'].get('qa-v3-run')==RUN and d['Name']=='/'+NAME
        cmd(['docker','rm','--force',CID])
        state['container']=dict(id=CID,name=NAME,owned_label_verified=True,removed=True)
    save('cleanup.json',state)
    before=json.loads((OUT/'original-integrity-before.json').read_text(encoding='utf-8'))
    after=fingerprints()
    changed=[p for p,h in before.items() if after.get(p)!=h]
    save('original-integrity-after.json',dict(hashes=after,changed=changed,preserved=not changed))
    say('ENCERRAMENTO '+json.dumps(state)+'; originais_preservados='+str(not changed))

if __name__=='__main__':
    if (OUT/'initial-state.json').exists(): raise SystemExit('Pasta ja contem execucao; copiar roteiro para nova pasta para reproduzir.')
    LOG=open(OUT/'execution.log','w',encoding='utf-8')
    try:
        jwt=prepare()
        execute(jwt)
        save('final-state.json',snap())
        from collections import Counter
        say('TOTAIS '+json.dumps(dict(Counter(r['classification'] for r in RESULTS)),ensure_ascii=False))
    except Exception:
        save('execution-error.json',dict(error=clean(traceback.format_exc()),logical_tests_completed=len(RESULTS)))
        say(clean(traceback.format_exc()))
        if CID and TABLES:
            try: save('final-state.json',snap())
            except Exception: pass
        raise
    finally:
        cleanup()
        LOG.close()
