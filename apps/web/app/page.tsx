import TimelineDashboard from './components/TimelineDashboard'
import TopRightAuth from './components/TopRightAuth'

type Activity = {
  id?: string
  title: string
  author: string
  image?: string
}

type Character = {
  id?: string
  name: string
  author: string
  image?: string
}

const apiUrl = process.env.NEXT_PUBLIC_API_BASE_URL || process.env.API_URL

async function fetchList(url: string) {
  try {
    const response = await fetch(url, { next: { revalidate: 60 } })
    if (!response.ok) return null
    const json = await response.json()
    if (Array.isArray(json)) return json
    if (json && typeof json === 'object') {
      if (Array.isArray((json as { items?: unknown[] }).items)) return (json as { items: unknown[] }).items
      if ((json as { ok?: boolean; data?: unknown }).ok && (json as { data?: unknown }).data) {
        const data = (json as { data: unknown }).data
        if (Array.isArray(data)) return data
        if (Array.isArray((data as { items?: unknown[] }).items)) return (data as { items: unknown[] }).items
      }
    }
    return null
  } catch {
    return null
  }
}

function mapActivities(items: unknown[] | null): Activity[] | null {
  if (!items) return null
  return items.map((item, index) => {
    const data = item as Record<string, unknown>
    return {
      id: String(data.id ?? ''),
      title:
        String(
          data.title ??
          data.name ??
          data.summary ??
          data.label ??
          `Activity ${index + 1}`
        ),
      author: String(
        data.author ??
        data.creator_name ??
        data.user_name ??
        data.owner_name ??
        'Unknown'
      ),
      image: String(
        data.image ??
        data.image_url ??
        data.thumbnail_url ??
        data.cover_url ??
        data.media_url ??
        ''
      ),
    }
  })
}

function mapCharacters(items: unknown[] | null): Character[] | null {
  if (!items) return null
  return items.map((item, index) => {
    const data = item as Record<string, unknown>
    return {
      id: String(data.id ?? ''),
      name: String(
        data.name ??
        data.character_name ??
        data.title ??
        data.label ??
        `Character ${index + 1}`
      ),
      author: String(
        data.author ??
        data.owner_name ??
        data.user_name ??
        data.creator_name ??
        'Unknown'
      ),
      image: String(
        data.image ??
        data.image_url ??
        data.thumbnail_url ??
        data.avatar_url ??
        data.cover_url ??
        ''
      ),
    }
  })
}

export default async function Page() {
  let activities: Activity[] = []
  let recentCharacters: Character[] = []
  let activityFetchFailed = false
  let recentCharacterFetchFailed = false

  if (apiUrl) {
    const baseUrl = apiUrl.replace(/\/$/, '')
    const [activityItems, characterItems] = await Promise.all([
      fetchList(`${baseUrl}/activities`),
      fetchList(`${baseUrl}/characters`),
    ])
    const mappedActivities = mapActivities(activityItems)
    const mappedCharacters = mapCharacters(characterItems)
    if (mappedActivities) {
      activities = mappedActivities
    } else {
      activityFetchFailed = true
    }
    if (mappedCharacters) {
      recentCharacters = mappedCharacters
    } else {
      recentCharacterFetchFailed = true
    }
  } else {
    activityFetchFailed = true
    recentCharacterFetchFailed = true
  }

  return (
    <main>
      <TopRightAuth />
        <TimelineDashboard
          apiBaseUrl={apiUrl}
          initialActivities={activities}
          initialRecentCharacters={recentCharacters}
          initialActivityError={activityFetchFailed}
          initialRecentCharacterError={recentCharacterFetchFailed}
        />
    </main>
  )
}
