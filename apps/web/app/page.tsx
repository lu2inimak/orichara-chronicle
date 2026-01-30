type Activity = {
  title: string
  author: string
  image?: string
}

type Character = {
  name: string
  author: string
  image?: string
}

const sampleCharacters: Character[] = [
  {
    name: 'Rin Aster',
    image: 'glass archive portrait',
    author: 'Yuna K.',
  },
  {
    name: 'Kael Dusk',
    image: 'storm navigator profile',
    author: 'Haruto S.',
  },
  {
    name: 'Mira Vale',
    image: 'obsidian relay character',
    author: 'Nagi T.',
  },
]

const sampleActivities: Activity[] = [
  {
    title: 'Twin moons observatory log',
    image: 'twin moons archive',
    author: 'Rin Aster',
  },
  {
    title: 'Tidebreak treaty rehearsal',
    image: 'tidebreak council hall',
    author: 'Kael Dusk',
  },
  {
    title: 'Obsidian relay workshop',
    image: 'obsidian relay workshop',
    author: 'Mira Vale',
  },
]

const apiUrl = process.env.NEXT_PUBLIC_API_BASE_URL || process.env.API_URL

async function fetchList(url: string) {
  try {
    const response = await fetch(url, { next: { revalidate: 60 } })
    if (!response.ok) return null
    const json = await response.json()
    if (Array.isArray(json)) return json
    if (json && typeof json === 'object') {
      if (Array.isArray(json.items)) return json.items
      if (json.ok && json.data) {
        if (Array.isArray(json.data)) return json.data
        if (Array.isArray(json.data.items)) return json.data.items
      }
      if (Array.isArray(json.activities)) return json.activities
      if (Array.isArray(json.characters)) return json.characters
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
  let activities = sampleActivities
  let characters = sampleCharacters
  let activityFetchFailed = false
  let characterFetchFailed = false

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
      characters = mappedCharacters
    } else {
      characterFetchFailed = true
    }
  }

  return (
    <main>
      <div className="layout">
        <aside className="sidebar">
          <div className="brand">
            <span className="brand-mark">OCC</span>
            <div>
              <strong>Ori-Chara Chronicle</strong>
              <span>Context-first creative archive</span>
            </div>
          </div>
          <div className="search">
            <input placeholder="Search..." type="search" />
          </div>
          <nav className="sidebar-links">
            <a className="link-pill is-active" href="#">
              Activity timeline
            </a>
            <a className="link-pill" href="#">
              Characters
            </a>
            <a className="link-pill" href="#">
              Worlds
            </a>
            <a className="link-pill" href="#">
              Activities
            </a>
          </nav>
          <div className="sidebar-actions">
            <a className="btn secondary" href="#">
              Log in
            </a>
            <a className="btn secondary" href="#">
              Sign up
            </a>
            <a className="btn primary" href="#">
              My page
            </a>
          </div>
        </aside>

        <div className="content">
          <section className="section">
            <div className="container">
              <div className="section-header">
                <div>
                  <h2>Activity timeline</h2>
                  <p>Fresh logs across worlds, ready for review.</p>
                </div>
                <div className="filters">
                  <button className="filter-chip is-active" type="button">
                    All
                  </button>
                  <button className="filter-chip" type="button">
                    Illustration
                  </button>
                  <button className="filter-chip" type="button">
                    Writing
                  </button>
                  <button className="filter-chip" type="button">
                    Audio
                  </button>
                  <button className="filter-chip" type="button">
                    3D
                  </button>
                  <a className="btn secondary" href="#">
                    View all activities
                  </a>
                </div>
              </div>
              <div className="grid-4 compact">
                {activityFetchFailed ? (
                  <article className="card error-card">
                    <div>
                      <strong>Fetch failed</strong>
                      <span>Activities</span>
                    </div>
                  </article>
                ) : (
                  activities.map((activity) => (
                    <article
                      className="card animate-in delay-2"
                      key={activity.title}
                    >
                      <div className="card-media">
                        <div
                          className="media-frame"
                          style={{
                            '--media-image': activity.image
                              ? `url("${activity.image}")`
                              : 'none',
                          }}
                        />
                        <div className="media-overlay">
                          <strong>{activity.title}</strong>
                          <span>by {activity.author}</span>
                        </div>
                      </div>
                    </article>
                  ))
                )}
              </div>
            </div>
          </section>

          <section className="section">
            <div className="container">
              <div className="section-header">
                <div>
                  <h2>Recently updated characters</h2>
                  <p>Keep track of role shifts and new affiliations.</p>
                </div>
                <div className="filters">
                  <button className="filter-chip is-active" type="button">
                    All
                  </button>
                  <button className="filter-chip" type="button">
                    Active
                  </button>
                  <button className="filter-chip" type="button">
                    Pending
                  </button>
                  <button className="filter-chip" type="button">
                    Archived
                  </button>
                  <a className="btn secondary" href="#">
                    View all characters
                  </a>
                </div>
              </div>
              <div className="grid-4 compact">
                {characterFetchFailed ? (
                  <article className="card error-card">
                    <div>
                      <strong>Fetch failed</strong>
                      <span>Characters</span>
                    </div>
                  </article>
                ) : (
                  characters.map((character) => (
                    <article
                      className="card animate-in delay-2"
                      key={character.name}
                    >
                      <div className="card-media">
                        <div
                          className="media-frame"
                          style={{
                            '--media-image': character.image
                              ? `url("${character.image}")`
                              : 'none',
                          }}
                        />
                        <div className="media-overlay">
                          <strong>{character.name}</strong>
                          <span>by {character.author}</span>
                        </div>
                      </div>
                    </article>
                  ))
                )}
              </div>
            </div>
          </section>
        </div>
      </div>
    </main>
  )
}
