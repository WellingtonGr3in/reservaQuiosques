import { useEffect, useRef, useState, type FormEvent } from 'react';
import { Link, useNavigate, useParams } from 'react-router';
import { adminApi, ApiError } from '../api/client';
import type { AdminKioskInput } from '../api/types';
import { ErrorBox, InfoBox, Loading } from '../components/Feedback';
import { Icon } from '../components/Icon';
import { useAdminSessionGuard } from './AdminLayout';
import { adminInput, Card, Field, PageHeader, primaryButton, secondaryButton } from './ui';

const textarea =
  'w-full px-3 py-2 bg-[#ffffff] text-[#141b2b] text-sm border border-[#d1d5db] rounded focus:border-[#2e7d32] focus:ring-1 focus:ring-[#2e7d32] outline-none transition-colors';

interface FormState {
  name: string;
  description: string;
  capacity: string;
  dailyRate: string;
  photoUrl: string;
  usageRules: string;
  isActive: boolean;
  items: string;
  photos: string;
}

const EMPTY: FormState = {
  name: '',
  description: '',
  capacity: '',
  dailyRate: '',
  photoUrl: '',
  usageRules: 'Horário de utilização: das 08:00 às 22:00.',
  isActive: true,
  items: '',
  photos: '',
};

const lines = (text: string) => text.split('\n').map((l) => l.trim()).filter(Boolean);

// RF16: cadastrar (/admin/quiosques/novo) ou editar (/admin/quiosques/:id) um quiosque.
export function AdminKioskFormPage() {
  const { id } = useParams();
  const isNew = !id;
  const navigate = useNavigate();
  const [form, setForm] = useState<FormState>(EMPTY);
  const [loading, setLoading] = useState(!isNew);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<ApiError | null>(null);
  const [saved, setSaved] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [uploadError, setUploadError] = useState<string | null>(null);
  const coverInput = useRef<HTMLInputElement>(null);
  const galleryInput = useRef<HTMLInputElement>(null);

  // RF16: envia as imagens escolhidas no explorador de arquivos e devolve os endereços.
  const upload = async (files: File[]): Promise<string[]> => {
    if (files.length === 0) return [];
    setUploading(true);
    setUploadError(null);
    const urls: string[] = [];
    try {
      for (const file of files) urls.push(await adminApi.uploadImage(file));
    } catch (err) {
      setUploadError(err instanceof ApiError ? err.message : String(err));
    } finally {
      setUploading(false);
    }
    return urls;
  };
  useAdminSessionGuard(error?.status);

  useEffect(() => {
    if (isNew) return;
    adminApi
      .getKiosk(id)
      .then((k) =>
        setForm({
          name: k.name,
          description: k.description,
          capacity: String(k.capacity),
          dailyRate: String(k.dailyRate),
          photoUrl: k.photoUrl,
          usageRules: k.usageRules,
          isActive: k.isActive,
          items: k.items.join('\n'),
          photos: k.photos.join('\n'),
        }),
      )
      .catch((err) => setError(err instanceof ApiError ? err : new ApiError(0, String(err))))
      .finally(() => setLoading(false));
  }, [id, isNew]);

  if (loading) return <Loading />;

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setBusy(true);
    setError(null);
    setSaved(false);
    const data: AdminKioskInput = {
      name: form.name,
      description: form.description,
      capacity: Number(form.capacity),
      dailyRate: Number(form.dailyRate.replace(',', '.')),
      photoUrl: form.photoUrl,
      usageRules: form.usageRules,
      isActive: form.isActive,
      items: lines(form.items),
      photos: lines(form.photos),
    };
    try {
      if (isNew) {
        const created = await adminApi.createKiosk(data);
        navigate(`/admin/quiosques/${created.id}`, { replace: true });
      } else {
        await adminApi.updateKiosk(id, data);
        setSaved(true);
      }
    } catch (err) {
      setError(err instanceof ApiError ? err : new ApiError(0, String(err)));
    } finally {
      setBusy(false);
    }
  };

  const set = (field: keyof FormState) => (e: { target: { value: string } }) => setForm({ ...form, [field]: e.target.value });

  return (
    <div className="max-w-[860px] mx-auto px-4 sm:px-6 py-8">
      <Link to="/admin/quiosques" className="flex items-center gap-1 text-xs text-[#40493d] hover:text-[#0d631b] mb-4">
        <Icon name="arrow_back" className="text-[16px]" /> Quiosques
      </Link>
      <PageHeader title={isNew ? 'Novo quiosque' : `Editar ${form.name}`} />

      <form onSubmit={submit} className="flex flex-col gap-5">
        <Card>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Field label="Nome" htmlFor="k-name">
              <input id="k-name" required maxLength={100} value={form.name} onChange={set('name')} className={adminInput} />
            </Field>
            <div className="grid grid-cols-2 gap-3">
              <Field label="Capacidade (pessoas)" htmlFor="k-capacity">
                <input id="k-capacity" type="number" required min={1} max={1000} value={form.capacity} onChange={set('capacity')} className={adminInput} />
              </Field>
              <Field label="Diária (R$)" htmlFor="k-rate">
                <input id="k-rate" type="number" required min={0.01} step={0.01} value={form.dailyRate} onChange={set('dailyRate')} className={adminInput} />
              </Field>
            </div>
            <div className="sm:col-span-2">
              <Field label="Descrição" htmlFor="k-description">
                <textarea id="k-description" required rows={3} value={form.description} onChange={set('description')} className={textarea} />
              </Field>
            </div>
            <div className="sm:col-span-2">
              <Field label="Regras de uso" htmlFor="k-rules">
                <textarea id="k-rules" required rows={3} value={form.usageRules} onChange={set('usageRules')} className={textarea} />
              </Field>
            </div>
            <label className="sm:col-span-2 flex items-center gap-2 text-sm cursor-pointer">
              <input type="checkbox" checked={form.isActive} onChange={(e) => setForm({ ...form, isActive: e.target.checked })} className="w-4 h-4 accent-[#0d631b]" />
              <span>
                <strong>Ativo</strong> — aparece para os clientes e aceita novas reservas
              </span>
            </label>
          </div>
        </Card>

        <Card>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Field label="Itens inclusos" htmlFor="k-items" hint="Um item por linha.">
              <textarea id="k-items" rows={5} value={form.items} onChange={set('items')} className={textarea} placeholder={'Grelha e espetos\nPia com água tratada'} />
            </Field>
            <Field label="Fotos da galeria" htmlFor="k-photos" hint="Envie do computador ou cole um endereço por linha, na ordem de exibição.">
              <textarea id="k-photos" rows={5} value={form.photos} onChange={set('photos')} className={`${textarea} font-mono text-xs`} />
              <button type="button" disabled={uploading} onClick={() => galleryInput.current?.click()} className={`${secondaryButton} mt-1 self-start`}>
                <Icon name="add_photo_alternate" className="text-[18px]" />
                {uploading ? 'Enviando...' : 'Adicionar fotos'}
              </button>
              <input
                ref={galleryInput}
                type="file"
                accept="image/jpeg,image/png,image/webp"
                multiple
                hidden
                onChange={async (e) => {
                  const files = Array.from(e.target.files ?? []);
                  e.target.value = '';
                  const urls = await upload(files);
                  if (urls.length > 0) setForm((f) => ({ ...f, photos: [...lines(f.photos), ...urls].join('\n') }));
                }}
              />
            </Field>

            <div className="sm:col-span-2">
              <Field label="Foto de capa (lista de quiosques)" htmlFor="k-photo" hint="Envie do computador ou informe o endereço, ex.: /imagens/quiosques/quiosque-a.jpg">
                <div className="flex gap-2">
                  <input id="k-photo" required maxLength={500} value={form.photoUrl} onChange={set('photoUrl')} className={`${adminInput} font-mono text-xs`} />
                  <button type="button" disabled={uploading} onClick={() => coverInput.current?.click()} className={`${secondaryButton} shrink-0`}>
                    <Icon name="upload" className="text-[18px]" />
                    <span className="hidden sm:inline">Escolher imagem</span>
                  </button>
                </div>
              </Field>
              <input
                ref={coverInput}
                type="file"
                accept="image/jpeg,image/png,image/webp"
                hidden
                onChange={async (e) => {
                  const files = Array.from(e.target.files ?? []);
                  e.target.value = '';
                  const [url] = await upload(files);
                  if (url) setForm((f) => ({ ...f, photoUrl: url }));
                }}
              />
            </div>

            {uploadError && (
              <div className="sm:col-span-2">
                <ErrorBox message={uploadError} />
              </div>
            )}

            <div className="sm:col-span-2 flex gap-3 overflow-x-auto pb-1">
              {form.photoUrl && (
                <figure className="shrink-0">
                  <img src={form.photoUrl} alt="Capa" className="h-20 w-28 rounded object-cover border-2 border-[#2e7d32] bg-[#e9edff]" />
                  <figcaption className="text-[10px] text-[#40493d] mt-0.5">Capa</figcaption>
                </figure>
              )}
              {lines(form.photos).map((url, i) => (
                <figure key={`${url}-${i}`} className="relative shrink-0">
                  <img src={url} alt={`Foto ${i + 1}`} className="h-20 w-28 rounded object-cover border border-[#dce2f7] bg-[#e9edff]" />
                  <button
                    type="button"
                    title="Remover da galeria"
                    onClick={() => setForm((f) => ({ ...f, photos: lines(f.photos).filter((_, j) => j !== i).join('\n') }))}
                    className="absolute top-1 right-1 w-5 h-5 rounded-full bg-[#141b2b]/70 text-white flex items-center justify-center hover:bg-[#ba1a1a]"
                  >
                    <Icon name="close" className="text-[14px]" />
                  </button>
                  <figcaption className="text-[10px] text-[#40493d] mt-0.5">Galeria {i + 1}</figcaption>
                </figure>
              ))}
            </div>
          </div>
        </Card>

        {saved && <InfoBox message="Quiosque salvo. Reservas já feitas mantêm o valor do momento da reserva." />}
        {error && <ErrorBox message={error.message} />}

        <div className="flex justify-end gap-2">
          <Link to="/admin/quiosques" className={secondaryButton}>
            Voltar
          </Link>
          <button type="submit" disabled={busy} className={primaryButton}>
            {busy ? 'Salvando...' : isNew ? 'Cadastrar quiosque' : 'Salvar alterações'}
          </button>
        </div>
      </form>
    </div>
  );
}
