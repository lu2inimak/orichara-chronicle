'use client'

import { useRef, useState } from 'react'

export default function PostPage() {
  const [title, setTitle] = useState('')
  const [content, setContent] = useState('')
  const [imageUrl, setImageUrl] = useState('')
  const [fileName, setFileName] = useState<string | null>(null)
  const [characters, setCharacters] = useState('')
  const [world, setWorld] = useState('')
  const [affiliationId, setAffiliationId] = useState('')
  const [status, setStatus] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const fileRef = useRef<HTMLInputElement | null>(null)

  const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL ?? ''

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!apiBaseUrl) {
      setStatus('API base URL is not set.')
      return
    }
    const normalizedAff = affiliationId.trim()
    if (!normalizedAff) {
      setStatus('Affiliation ID is required.')
      return
    }
    setSubmitting(true)
    setStatus(null)
    try {
      const username = window.localStorage.getItem('occ_username') ?? ''
      const content = [title.trim(), content.trim()].filter(Boolean).join('\n\n')
      const coCreators = characters
        .split(',')
        .map((value) => value.trim())
        .filter((value) => value.length > 0)
      const response = await fetch(`${apiBaseUrl.replace(/\/$/, '')}/activities`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: username ? `Bearer ${username}` : '',
        },
        body: JSON.stringify({
          affiliation_id: normalizedAff,
          content: content || 'Untitled activity',
          co_creators: coCreators,
        }),
      })
      if (!response.ok) {
        setStatus(`Failed: ${response.status}`)
        return
      }
      setStatus('Saved.')
      setTitle('')
      setContent('')
      setImageUrl('')
      setCharacters('')
      setWorld('')
      setAffiliationId('')
    } catch {
      setStatus('Failed to post.')
    } finally {
      setSubmitting(false)
    }
  }

  const handleUpload = async () => {
    const file = fileRef.current?.files?.[0]
    if (!file) {
      setStatus('Select an image first.')
      return
    }
    if (!apiBaseUrl) {
      setStatus('API base URL is not set.')
      return
    }
    setStatus('Uploading...')
    try {
      const presign = await fetch(`${apiBaseUrl.replace(/\/$/, '')}/uploads/presign`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          file_name: file.name,
          content_type: file.type,
          prefix: 'activities',
        }),
      })
      if (!presign.ok) {
        setStatus(`Presign failed: ${presign.status}`)
        return
      }
      const payload = (await presign.json()) as { data?: { upload_url?: string; public_url?: string } }
      const uploadUrl = payload.data?.upload_url ?? ''
      const publicUrl = payload.data?.public_url ?? ''
      if (!uploadUrl) {
        setStatus('Upload URL missing.')
        return
      }
      const response = await fetch(uploadUrl, {
        method: 'PUT',
        headers: {
          'Content-Type': file.type || 'application/octet-stream',
        },
        body: file,
      })
      if (!response.ok) {
        setStatus(`Upload failed: ${response.status}`)
        return
      }
      if (publicUrl) {
        setImageUrl(publicUrl)
      }
      setFileName(file.name)
      setStatus('Uploaded.')
    } catch {
      setStatus('Upload failed.')
    }
  }

  return (
    <main className="auth-page">
      <div className="auth-card">
        <h1>New Activity</h1>
        <p>Post a new activity log.</p>
        <form onSubmit={handleSubmit}>
          <label>
            Image File
            <input ref={fileRef} name="image_file" type="file" accept="image/*" />
          </label>
          <button className="btn secondary" type="button" onClick={handleUpload}>
            Upload to S3
          </button>
          {fileName ? <span className="auth-note">Uploaded: {fileName}</span> : null}
          <label>
            Affiliation ID
            <input
              name="affiliation_id"
              value={affiliationId}
              onChange={(event) => setAffiliationId(event.target.value)}
              placeholder="AFF-..."
              required
            />
          </label>
          <label>
            Title
            <input
              name="title"
              value={title}
              onChange={(event) => setTitle(event.target.value)}
              placeholder="Activity title"
            />
          </label>
          <label>
            Content
            <textarea
              name="content"
              value={content}
              onChange={(event) => setContent(event.target.value)}
              placeholder="Write the log..."
              rows={5}
            />
          </label>
          <label>
            Image URL
            <input
              name="image_url"
              value={imageUrl}
              onChange={(event) => setImageUrl(event.target.value)}
              placeholder="https://"
            />
          </label>
          <label>
            Characters
            <input
              name="characters"
              value={characters}
              onChange={(event) => setCharacters(event.target.value)}
              placeholder="Comma separated"
            />
          </label>
          <label>
            World
            <input
              name="world"
              value={world}
              onChange={(event) => setWorld(event.target.value)}
              placeholder="World name"
            />
          </label>
          <button className="btn primary" type="submit">
            {submitting ? 'Saving...' : 'Save'}
          </button>
          {status ? <span className="auth-note">{status}</span> : null}
        </form>
      </div>
    </main>
  )
}
